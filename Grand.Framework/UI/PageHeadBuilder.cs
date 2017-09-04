﻿using Grand.Core.Domain.Seo;
using Grand.Core.Infrastructure;
using Grand.Framework.Themes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grand.Framework.UI
{
    /// <summary>
    /// Page head builder
    /// </summary>
    public partial class PageHeadBuilder : IPageHeadBuilder
    {
        #region Fields

        private static readonly object s_lock = new object();

        private readonly SeoSettings _seoSettings;
        private readonly IHostingEnvironment _hostingEnvironment;

        private readonly List<string> _titleParts;
        private readonly List<string> _metaDescriptionParts;
        private readonly List<string> _metaKeywordParts;
        private readonly Dictionary<ResourceLocation, List<ScriptReferenceMeta>> _scriptParts;
        private readonly Dictionary<ResourceLocation, List<CssReferenceMeta>> _cssParts;
        private readonly List<string> _canonicalUrlParts;
        private readonly List<string> _headCustomParts;
        private readonly List<string> _pageCssClassParts;
        private string _editPageUrl;
        private string _activeAdminMenuSystemName;

        #endregion

        #region Ctor

        /// <summary>
        /// Constuctor
        /// </summary>
        /// <param name="seoSettings">SEO settings</param>
        /// <param name="hostingEnvironment">Hosting environment</param>
        public PageHeadBuilder(SeoSettings seoSettings, IHostingEnvironment hostingEnvironment)
        {
            this._seoSettings = seoSettings;
            this._hostingEnvironment = hostingEnvironment;

            this._titleParts = new List<string>();
            this._metaDescriptionParts = new List<string>();
            this._metaKeywordParts = new List<string>();
            this._scriptParts = new Dictionary<ResourceLocation, List<ScriptReferenceMeta>>();
            this._cssParts = new Dictionary<ResourceLocation, List<CssReferenceMeta>>();
            this._canonicalUrlParts = new List<string>();
            this._headCustomParts = new List<string>();
            this._pageCssClassParts = new List<string>();
        }

        #endregion

        #region Methods

        public virtual void AddTitleParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _titleParts.Add(part);
        }
        public virtual void AppendTitleParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
            
            _titleParts.Insert(0, part);
        }
        public virtual string GenerateTitle(bool addDefaultTitle)
        {
            string result = "";
            var specificTitle = string.Join(_seoSettings.PageTitleSeparator, _titleParts.AsEnumerable().Reverse().ToArray());
            if (!String.IsNullOrEmpty(specificTitle))
            {
                if (addDefaultTitle)
                {
                    //store name + page title
                    switch (_seoSettings.PageTitleSeoAdjustment)
                    {
                        case PageTitleSeoAdjustment.PagenameAfterStorename:
                            {
                                result = string.Join(_seoSettings.PageTitleSeparator, _seoSettings.DefaultTitle, specificTitle);
                            }
                            break;
                        case PageTitleSeoAdjustment.StorenameAfterPagename:
                        default:
                            {
                                result = string.Join(_seoSettings.PageTitleSeparator, specificTitle, _seoSettings.DefaultTitle);
                            }
                            break;
                            
                    }
                }
                else
                {
                    //page title only
                    result = specificTitle;
                }
            }
            else
            {
                //store name only
                result = _seoSettings.DefaultTitle;
            }
            return result;
        }


        public virtual void AddMetaDescriptionParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
            
            _metaDescriptionParts.Add(part);
        }
        public virtual void AppendMetaDescriptionParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
            
            _metaDescriptionParts.Insert(0, part);
        }
        public virtual string GenerateMetaDescription()
        {
            var metaDescription = string.Join(", ", _metaDescriptionParts.AsEnumerable().Reverse().ToArray());
            var result = !String.IsNullOrEmpty(metaDescription) ? metaDescription : _seoSettings.DefaultMetaDescription;
            return result;
        }


        public virtual void AddMetaKeywordParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
            
            _metaKeywordParts.Add(part);
        }
        public virtual void AppendMetaKeywordParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _metaKeywordParts.Insert(0, part);
        }
        public virtual string GenerateMetaKeywords()
        {
            var metaKeyword = string.Join(", ", _metaKeywordParts.AsEnumerable().Reverse().ToArray());
            var result = !String.IsNullOrEmpty(metaKeyword) ? metaKeyword : _seoSettings.DefaultMetaKeywords;
            return result;
        }
    

        public virtual void AddScriptParts(ResourceLocation location, string src, string debugSrc, bool excludeFromMinification, bool isAsync)
        {
            if (!_scriptParts.ContainsKey(location))
                _scriptParts.Add(location, new List<ScriptReferenceMeta>());

            if (string.IsNullOrEmpty(src))
                return;

            if (String.IsNullOrEmpty(debugSrc))
                debugSrc = src;

            _scriptParts[location].Add(new ScriptReferenceMeta
            {
                ExcludeFromMinification = excludeFromMinification,
                IsAsync = isAsync,
                Src = src,
                DebugSrc = debugSrc
            });
        }
        public virtual void AppendScriptParts(ResourceLocation location, string src, string debugSrc, bool excludeFromMinification, bool isAsync)
        {
            if (!_scriptParts.ContainsKey(location))
                _scriptParts.Add(location, new List<ScriptReferenceMeta>());

            if (string.IsNullOrEmpty(src))
                return;

            if (String.IsNullOrEmpty(debugSrc))
                debugSrc = src;

            _scriptParts[location].Insert(0, new ScriptReferenceMeta
            {
                ExcludeFromMinification = excludeFromMinification,
                IsAsync = isAsync,
                Src = src,
                DebugSrc = debugSrc
            });
        }

        public virtual string GenerateScripts(IUrlHelper urlHelper, ResourceLocation location)
        {
            if (!_scriptParts.ContainsKey(location) || _scriptParts[location] == null)
                return "";

            if (!_scriptParts.Any())
                return "";

            var debugModel = _hostingEnvironment.IsDevelopment();

            var minifyFiles = _seoSettings.EnableJsMinification;

            if (minifyFiles)
            {
                var partsToMinify = _scriptParts[location]
                    .Where(x => !x.ExcludeFromMinification)
                    .Distinct()
                    .ToArray();
                var partsToDontMinify = _scriptParts[location]
                    .Where(x => x.ExcludeFromMinification)
                    .Distinct()
                    .ToArray();

                var result = new StringBuilder();

                //parts to minify
                foreach (var item in partsToMinify)
                {
                    var src = debugModel ? item.DebugSrc : item.Src;
                    if (src.Contains("/Scripts") && src.Contains("public."))
                        src = src.Insert(src.IndexOf("/Scripts"), "/Minified");

                    result.AppendFormat("<script {2}src=\"{0}\" type=\"{1}\"></script>", urlHelper.Content(src), "text/javascript", item.IsAsync ? "async " : "");
                    result.Append(Environment.NewLine);
                }

                //parts to not minify
                foreach (var item in partsToDontMinify)
                {
                    var src = debugModel ? item.DebugSrc : item.Src;
                    result.AppendFormat("<script {2}src=\"{0}\" type=\"{1}\"></script>", urlHelper.Content(src), "text/javascript", item.IsAsync ? "async " : "");
                    result.Append(Environment.NewLine);
                }
                return result.ToString();
            }
            else
            {
                //minifying is disabled
                var result = new StringBuilder();
                foreach (var item in _scriptParts[location].Distinct())
                {
                    var src = debugModel ? item.DebugSrc : item.Src;
                    result.AppendFormat("<script {2}src=\"{0}\" type=\"{1}\"></script>", urlHelper.Content(src), "text/javascript", item.IsAsync ? "async " : "");
                    result.Append(Environment.NewLine);
                }
                return result.ToString();
            }
        }

        public virtual void AddCssFileParts(ResourceLocation location, string src, string debugSrc, bool excludeFromMinification = false)
        {
            if (!_cssParts.ContainsKey(location))
                _cssParts.Add(location, new List<CssReferenceMeta>());

            if (string.IsNullOrEmpty(src))
                return;

            if (String.IsNullOrEmpty(debugSrc))
                debugSrc = src;

            _cssParts[location].Add(new CssReferenceMeta
            {
                ExcludeFromMinification = excludeFromMinification,
                Src = src,
                DebugSrc = debugSrc
            });
        }
        public virtual void AppendCssFileParts(ResourceLocation location, string src, string debugSrc, bool excludeFromMinification = false)
        {
            if (!_cssParts.ContainsKey(location))
                _cssParts.Add(location, new List<CssReferenceMeta>());

            if (string.IsNullOrEmpty(src))
                return;

            if (String.IsNullOrEmpty(debugSrc))
                debugSrc = src;

            _cssParts[location].Insert(0, new CssReferenceMeta
            {
                ExcludeFromMinification = excludeFromMinification,
                Src = src,
                DebugSrc = debugSrc
            });
        }

        public virtual string GenerateCssFiles(IUrlHelper urlHelper, ResourceLocation location)
        {
            if (!_cssParts.ContainsKey(location) || _cssParts[location] == null)
                return "";

            if (!_cssParts.Any())
                return "";

            var debugModel = _hostingEnvironment.IsDevelopment();

            var minifyFiles = _seoSettings.EnableCssMinification;

            if (minifyFiles)
            {
                var partsToMinify = _cssParts[location]
                    .Where(x => !x.ExcludeFromMinification)
                    .Distinct()
                    .ToArray();
                var partsToDontMinify = _cssParts[location]
                    .Where(x => x.ExcludeFromMinification)
                    .Distinct()
                    .ToArray();

                var result = new StringBuilder();

                //parts to minify
                foreach (var item in partsToMinify)
                {
                    var src = debugModel ? item.DebugSrc : item.Src;
                    var themeName = EngineContext.Current.Resolve<IThemeContext>().WorkingThemeName;
                    var key = "/Themes/" + themeName + "/Content/css/";
                    if (src.Contains(key))
                        src = src.Insert(src.IndexOf(key), "/Minified");
                    result.AppendFormat("<link href=\"{0}\" rel=\"stylesheet\" type=\"{1}\" />", urlHelper.Content(src), "text/css");
                    result.Append(Environment.NewLine);
                }

                //parts to not minify
                foreach (var item in partsToDontMinify)
                {
                    var src = debugModel ? item.DebugSrc : item.Src;
                    result.AppendFormat("<link href=\"{0}\" rel=\"stylesheet\" type=\"{1}\" />", urlHelper.Content(src), "text/css");
                    result.Append(Environment.NewLine);
                }

                return result.ToString();
            }
            else
            {
                //minifying is disabled
                var result = new StringBuilder();
                foreach (var item in _cssParts[location].Distinct())
                {
                    var src = debugModel ? item.DebugSrc : item.Src;
                    result.AppendFormat("<link href=\"{0}\" rel=\"stylesheet\" type=\"{1}\" />", urlHelper.Content(src), "text/css");
                    result.AppendLine();
                }
                return result.ToString();
            }
        }

        public virtual void AddCanonicalUrlParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
                       
            _canonicalUrlParts.Add(part);
        }
        public virtual void AppendCanonicalUrlParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
                       
            _canonicalUrlParts.Insert(0, part);
        }
        public virtual string GenerateCanonicalUrls()
        {
            var result = new StringBuilder();
            foreach (var canonicalUrl in _canonicalUrlParts)
            {
                result.AppendFormat("<link rel=\"canonical\" href=\"{0}\" />", canonicalUrl);
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }


        public virtual void AddHeadCustomParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _headCustomParts.Add(part);
        }
        public virtual void AppendHeadCustomParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _headCustomParts.Insert(0, part);
        }
        public virtual string GenerateHeadCustom()
        {
            //use only distinct rows
            var distinctParts = _headCustomParts.Distinct().ToList();
            if (!distinctParts.Any())
                return "";

            var result = new StringBuilder();
            foreach (var path in distinctParts)
            {
                result.Append(path);
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }

        
        public virtual void AddPageCssClassParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _pageCssClassParts.Add(part);
        }
        public virtual void AppendPageCssClassParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _pageCssClassParts.Insert(0, part);
        }
        public virtual string GeneratePageCssClasses()
        {
            string result = string.Join(" ", _pageCssClassParts.AsEnumerable().Reverse().ToArray());
            return result;
        }


        /// <summary>
        /// Specify "edit page" URL
        /// </summary>
        /// <param name="url">URL</param>
        public virtual void AddEditPageUrl(string url)
        {
            _editPageUrl = url;
        }
        /// <summary>
        /// Get "edit page" URL
        /// </summary>
        /// <returns>URL</returns>
        public virtual string GetEditPageUrl()
        {
            return _editPageUrl;
        }


        /// <summary>
        /// Specify system name of admin menu item that should be selected (expanded)
        /// </summary>
        /// <param name="systemName">System name</param>
        public virtual void SetActiveMenuItemSystemName(string systemName)
        {
            _activeAdminMenuSystemName = systemName;
        }
        /// <summary>
        /// Get system name of admin menu item that should be selected (expanded)
        /// </summary>
        /// <returns>System name</returns>
        public virtual string GetActiveMenuItemSystemName()
        {
            return _activeAdminMenuSystemName;
        }

        #endregion
        
        #region Nested classes

        private class ScriptReferenceMeta
        {
            public bool ExcludeFromMinification { get; set; }

            public bool IsAsync { get; set; }

            public string Src { get; set; }

            public string DebugSrc { get; set; }
        }

        private class CssReferenceMeta
        {
            public bool ExcludeFromMinification { get; set; }

            public string Src { get; set; }

            public string DebugSrc { get; set; }
        }
        #endregion
    }
}
