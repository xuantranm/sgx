#pragma checksum "C:\Projects\Tribat\sourcecode\erp\Views\Account\NotFound.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "c2baffd865d317bd356e0ce19ef7b81c8d15af31"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Account_NotFound), @"mvc.1.0.view", @"/Views/Account/NotFound.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/Account/NotFound.cshtml", typeof(AspNetCore.Views_Account_NotFound))]
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
#line 1 "C:\Projects\Tribat\sourcecode\erp\Views\_ViewImports.cshtml"
using Models;

#line default
#line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"c2baffd865d317bd356e0ce19ef7b81c8d15af31", @"/Views/Account/NotFound.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"55f7d09cb6699920b8416cd86872bb94362cdab7", @"/Views/_ViewImports.cshtml")]
    public class Views_Account_NotFound : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("href", new global::Microsoft.AspNetCore.Html.HtmlString("~/css/error/integrity-light.css"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("rel", new global::Microsoft.AspNetCore.Html.HtmlString("stylesheet"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("class", new global::Microsoft.AspNetCore.Html.HtmlString(@"page-template page-template-template-blank-6 page-template-template-blank-6-php page page-id-14510 x-integrity x-integrity-light x-navbar-fixed-top-active x-full-width-layout-active x-content-sidebar-active x-post-meta-disabled wpb-js-composer js-comp-ver-4.3.5 vc_responsive x-Tribat_2_3 x-child-theme-active x-shortcodes-Tribat_0_5"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper;
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.BodyTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_BodyTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.RenderAtEndOfFormTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(0, 2, true);
            WriteLiteral("\r\n");
            EndContext();
#line 2 "C:\Projects\Tribat\sourcecode\erp\Views\Account\NotFound.cshtml"
  
    ViewData["Title"] = "Page Not Found";

#line default
#line hidden
            BeginContext(52, 2, true);
            WriteLiteral("\r\n");
            EndContext();
#line 6 "C:\Projects\Tribat\sourcecode\erp\Views\Account\NotFound.cshtml"
  
    Layout = "";
    ViewData["Title"] = "Oh no, you do not have access to this resource!";
    ViewData["Description"] = "This page not exist. Data maybe deleted.";

#line default
#line hidden
            BeginContext(230, 2, true);
            WriteLiteral("\r\n");
            EndContext();
            BeginContext(232, 64, false);
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("link", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "75a1288e9b0345c7bcc00246bb9120e1", async() => {
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            EndContext();
            BeginContext(296, 2, true);
            WriteLiteral("\r\n");
            EndContext();
            BeginContext(298, 2589, false);
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("body", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "076fdf8020c7406eac2c5d08ec152989", async() => {
                BeginContext(646, 937, true);
                WriteLiteral(@"
    <div id=""top"" class=""site"">
        <div class=""x-main full"" role=""main"">
            <article id=""post-14510"" class=""post-14510 page type-page status-publish hentry no-post-thumbnail"">
                <div class=""entry-content content"">
                    <div id=""x-content-band-2"" class=""x-content-band vc bg-image border-top man"" data-x-element=""content_band"" data-x-params=""{&quot;type&quot;:&quot;image&quot;,&quot;parallax&quot;:false}"" style=""background-image: url(/images/404.jpg); background-color: #272727; padding-top: 15%; padding-bottom: 30%;"">
                        <div class=""x-container wpb_row"">
                            <div class=""x-column x-sm vc x-1-1"" style="""">
                                <div class=""x-container max width center-text"">
                                    <h3 class=""man w-800"" style=""color: #fff; text-shadow:1px 1px 4px #000;"">
                                        ");
                EndContext();
                BeginContext(1584, 17, false);
#line 23 "C:\Projects\Tribat\sourcecode\erp\Views\Account\NotFound.cshtml"
                                   Write(ViewData["Title"]);

#line default
#line hidden
                EndContext();
                BeginContext(1601, 232, true);
                WriteLiteral("\r\n                                    </h3>\r\n                                    <h4 style=\"color: #fff; text-shadow:1px 1px 4px #000;\">\r\n                                        <strong>\r\n                                            ");
                EndContext();
                BeginContext(1834, 23, false);
#line 27 "C:\Projects\Tribat\sourcecode\erp\Views\Account\NotFound.cshtml"
                                       Write(ViewData["Description"]);

#line default
#line hidden
                EndContext();
                BeginContext(1857, 132, true);
                WriteLiteral("\r\n                                        </strong>\r\n                                    </h4>\r\n                                    ");
                EndContext();
                BeginContext(1989, 170, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("form", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "39d92f81550744859b89113be72b5273", async() => {
                    BeginContext(1995, 157, true);
                    WriteLiteral("\r\n                                        <input type=\"button\" value=\"No, really, go back!\" onclick=\"history.go(-1)\" />\r\n                                    ");
                    EndContext();
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper);
                __Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.RenderAtEndOfFormTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Write(__tagHelperExecutionContext.Output);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                EndContext();
                BeginContext(2159, 721, true);
                WriteLiteral(@"
                                    <a class=""x-btn mbm x-btn-flat x-btn-square x-btn-x-large"" style=""color: #fff;border-color: #00a94f;background-color: #73c167;"" href=""/"" title=""Home page"">Back to Homepage</a>
                                    <a class=""x-btn mbm x-btn-flat x-btn-square x-btn-x-large"" href=""/manage"" title=""Authorization"">My authorization</a>
                                    <a class=""x-btn mbm x-btn-flat x-btn-square x-btn-x-large"" href=""/support"" title=""Support"">Support</a>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </article>
        </div>
    </div>
");
                EndContext();
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_BodyTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.BodyTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_BodyTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_2);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            EndContext();
            BeginContext(2887, 4, true);
            WriteLiteral("\r\n\r\n");
            EndContext();
        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
