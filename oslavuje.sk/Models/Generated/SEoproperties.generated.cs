//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    Umbraco.ModelsBuilder.Embedded v13.7.0+eaea7a6
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Infrastructure.ModelsBuilder;
using Umbraco.Cms.Core;
using Umbraco.Extensions;

namespace Umbraco.Cms.Web.Common.PublishedModels
{
	// Mixin Content Type with alias "sEOProperties"
	/// <summary>SEO Properties</summary>
	public partial interface ISEoproperties : IPublishedElement
	{
		/// <summary>Is Followable</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		bool IsFollowable { get; }

		/// <summary>Is Indexable</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		bool IsIndexable { get; }

		/// <summary>Meta Description</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		[global::System.Diagnostics.CodeAnalysis.MaybeNull]
		string MetaDescription { get; }

		/// <summary>Meta Title</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		[global::System.Diagnostics.CodeAnalysis.MaybeNull]
		string MetaTitle { get; }
	}

	/// <summary>SEO Properties</summary>
	[PublishedModel("sEOProperties")]
	public partial class SEoproperties : PublishedElementModel, ISEoproperties
	{
		// helpers
#pragma warning disable 0109 // new is redundant
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		public new const string ModelTypeAlias = "sEOProperties";
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		public new const PublishedItemType ModelItemType = PublishedItemType.Content;
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		[return: global::System.Diagnostics.CodeAnalysis.MaybeNull]
		public new static IPublishedContentType GetModelContentType(IPublishedSnapshotAccessor publishedSnapshotAccessor)
			=> PublishedModelUtility.GetModelContentType(publishedSnapshotAccessor, ModelItemType, ModelTypeAlias);
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		[return: global::System.Diagnostics.CodeAnalysis.MaybeNull]
		public static IPublishedPropertyType GetModelPropertyType<TValue>(IPublishedSnapshotAccessor publishedSnapshotAccessor, Expression<Func<SEoproperties, TValue>> selector)
			=> PublishedModelUtility.GetModelPropertyType(GetModelContentType(publishedSnapshotAccessor), selector);
#pragma warning restore 0109

		private IPublishedValueFallback _publishedValueFallback;

		// ctor
		public SEoproperties(IPublishedElement content, IPublishedValueFallback publishedValueFallback)
			: base(content, publishedValueFallback)
		{
			_publishedValueFallback = publishedValueFallback;
		}

		// properties

		///<summary>
		/// Is Followable: Nastavte hodnotu na true, ak chcete, aby mohli roboti sledovať túto stránku.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		[ImplementPropertyType("isFollowable")]
		public virtual bool IsFollowable => GetIsFollowable(this, _publishedValueFallback);

		/// <summary>Static getter for Is Followable</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		public static bool GetIsFollowable(ISEoproperties that, IPublishedValueFallback publishedValueFallback) => that.Value<bool>(publishedValueFallback, "isFollowable");

		///<summary>
		/// Is Indexable: Nastavte hodnotu na true, ak chcete, aby mohli roboti indexovať túto stránku.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		[ImplementPropertyType("isIndexable")]
		public virtual bool IsIndexable => GetIsIndexable(this, _publishedValueFallback);

		/// <summary>Static getter for Is Indexable</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		public static bool GetIsIndexable(ISEoproperties that, IPublishedValueFallback publishedValueFallback) => that.Value<bool>(publishedValueFallback, "isIndexable");

		///<summary>
		/// Meta Description: Vložte meta description pre túto stránku.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		[global::System.Diagnostics.CodeAnalysis.MaybeNull]
		[ImplementPropertyType("metaDescription")]
		public virtual string MetaDescription => GetMetaDescription(this, _publishedValueFallback);

		/// <summary>Static getter for Meta Description</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		[return: global::System.Diagnostics.CodeAnalysis.MaybeNull]
		public static string GetMetaDescription(ISEoproperties that, IPublishedValueFallback publishedValueFallback) => that.Value<string>(publishedValueFallback, "metaDescription");

		///<summary>
		/// Meta Title: Vložte meta title pre túto stránku. Ak to zostane prázdne, použije sa meno stránky. Zobrazuje sa v záložke prehliadača.
		///</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		[global::System.Diagnostics.CodeAnalysis.MaybeNull]
		[ImplementPropertyType("metaTitle")]
		public virtual string MetaTitle => GetMetaTitle(this, _publishedValueFallback);

		/// <summary>Static getter for Meta Title</summary>
		[global::System.CodeDom.Compiler.GeneratedCodeAttribute("Umbraco.ModelsBuilder.Embedded", "13.7.0+eaea7a6")]
		[return: global::System.Diagnostics.CodeAnalysis.MaybeNull]
		public static string GetMetaTitle(ISEoproperties that, IPublishedValueFallback publishedValueFallback) => that.Value<string>(publishedValueFallback, "metaTitle");
	}
}
