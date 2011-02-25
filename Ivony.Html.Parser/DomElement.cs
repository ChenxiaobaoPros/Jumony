﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Ivony.Fluent;
using System.Globalization;

namespace Ivony.Html.Parser
{
  /// <summary>
  /// IHtmlElement 的实现
  /// </summary>
  public class DomElement : DomNode, IHtmlElement, IDomContainer
  {

    private readonly string _name;
    private readonly DomAttributeCollection _attributes;


    /// <summary>
    /// 创建 DomElement 实例
    /// </summary>
    /// <param name="name"></param>
    /// <param name="attributes"></param>
    public DomElement( string name, IDictionary<string, string> attributes )
    {
      if ( name == null )
        throw new ArgumentNullException( "name" );

      _name = name;
      _attributes = new DomAttributeCollection( this );

      if ( attributes != null )
        attributes.ForAll( item => _attributes.Add( new DomAttribute( this, item.Key, item.Value ) ) );
    }


    /// <summary>
    /// 获取一个名称，用于在抛出 ObjectDisposedException 异常时说明自己
    /// </summary>
    protected override string ObjectName
    {
      get { return "Element"; }
    }


    /// <summary>
    /// 获取元素名
    /// </summary>
    public string Name
    {
      get
      {
        CheckDisposed();

        return _name;
      }
    }


    /// <summary>
    /// 获取所有 HTML 属性
    /// </summary>
    /// <returns>所有属性对象</returns>
    public IEnumerable<IHtmlAttribute> Attributes()
    {
      CheckDisposed();

      return _attributes.Cast<IHtmlAttribute>().AsReadOnly();
    }


    /// <summary>
    /// 添加一个属性
    /// </summary>
    /// <param name="name">属性名</param>
    /// <param name="value">属性值</param>
    /// <returns>添加后的属性</returns>
    internal IHtmlAttribute AddAttribute( string name, string value )
    {
      lock ( SyncRoot )
      {
        CheckDisposed();

        if ( _attributes.Contains( name ) )//容器自身会执行不区分大小写的查找
          throw new InvalidOperationException( string.Format( CultureInfo.InvariantCulture, "元素已经存在名为 \"{0}\" 的属性。", name ) );

        var attribute = new DomAttribute( this, name, value );
        _attributes.Add( attribute );

        return attribute;
      }
    }

    protected class DomAttributeCollection : SynchronizedKeyedCollection<string, DomAttribute>
    {

      public DomAttributeCollection( DomElement element )
        : base( element.SyncRoot, StringComparer.OrdinalIgnoreCase )
      {
      }


      protected override string GetKeyForItem( DomAttribute item )
      {
        return item.Name.ToLowerInvariant();
      }
    }



    private DomNodeCollection _nodeCollection;

    /// <summary>
    /// 获取子节点容器
    /// </summary>
    public DomNodeCollection NodeCollection
    {
      get
      {
        lock ( SyncRoot )
        {
          if ( _nodeCollection == null )
            _nodeCollection = new DomNodeCollection( this );

          return _nodeCollection;
        }
      }
    }

    /// <summary>
    /// 获取所有子节点
    /// </summary>
    /// <returns>所有子节点列表</returns>
    public IEnumerable<IHtmlNode> Nodes()
    {
      lock ( SyncRoot )
      {
        if ( _nodeCollection == null )
          _nodeCollection = new DomNodeCollection( this );

        return _nodeCollection.HtmlNodes;
      }
    }



    public override void Remove()
    {
      this.ClearNodes();

      base.Remove();
    }

    internal void RemoveAttribute( DomAttribute attribute )
    {
      lock ( SyncRoot )
      {
        _attributes.Remove( attribute );
      }
    }
  }


  /// <summary>
  /// IHtmlAttribute 的实现
  /// </summary>
  public class DomAttribute : IHtmlAttribute, IHtmlDomObject
  {

    private readonly DomElement _element;
    private readonly string _name;
    private string _value;

    private bool disposed = false;


    /// <summary>
    /// 创建 DomAttribute 实例
    /// </summary>
    /// <param name="element">所属元素</param>
    /// <param name="name">属性名</param>
    /// <param name="value">属性值</param>
    public DomAttribute( DomElement element, string name, string value )
    {
      _element = element;
      _name = name;
      _value = value;
    }

    /// <summary>
    /// 判定两个属性对象是否相同
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals( object obj )
    {

      var attribute = obj as IHtmlAttribute;

      if ( attribute == null )
        return false;

      if ( !attribute.Element.Equals( this.Element ) )
        return false;

      if ( attribute.Name.EqualsIgnoreCase( this.Name ) && attribute.AttributeValue == this.AttributeValue )
        return true;

      return base.Equals( obj );

    }

    /// <summary>
    /// 用作特定类型的哈希函数
    /// </summary>
    /// <returns>当前对象的哈希代码</returns>
    public override int GetHashCode()
    {
      return Element.GetHashCode() ^ Name.ToLowerInvariant().GetHashCode() ^ AttributeValue.GetHashCode();
    }

    #region IHtmlAttribute 成员

    /// <summary>
    /// 获取属性所属的元素
    /// </summary>
    public DomElement Element
    {
      get
      {
        if ( disposed )
          throw new ObjectDisposedException( "Attribute" );

        return _element;
      }
    }


    IHtmlElement IHtmlAttribute.Element
    {
      get { return _element; }
    }


    /// <summary>
    /// 获取属性名
    /// </summary>
    public string Name
    {
      get
      {
        if ( disposed )
          throw new ObjectDisposedException( "Attribute" );

        return _name;
      }
    }

    /// <summary>
    /// 获取属性值
    /// </summary>
    public string AttributeValue
    {
      get
      {
        if ( disposed )
          throw new ObjectDisposedException( "Attribute" );

        return _value;
      }
      set
      {
        if ( disposed )
          throw new ObjectDisposedException( "Attribute" );

        lock ( Element.SyncRoot )
        {
          _value = value;
        }
      }
    }

    /// <summary>
    /// 尝试从元素中移除此属性
    /// </summary>
    public void Remove()
    {

      if ( disposed )
        throw new ObjectDisposedException( "Attribute" );

      lock ( Element.SyncRoot )
      {
        _element.RemoveAttribute( this );
        disposed = true;
      }
    }

    object IHtmlDomObject.RawObject
    {
      get { return this; }
    }

    public virtual string RawHtml
    {
      get { return null; }
    }

    IHtmlDocument IHtmlDomObject.Document
    {
      get { return Element.Document; }
    }


    private object _sync = new object();
    /// <summary>
    /// 获取用于同步的对象
    /// </summary>
    public object SyncRoot
    {
      get { return _sync; }
    }

    #endregion
  }

}
