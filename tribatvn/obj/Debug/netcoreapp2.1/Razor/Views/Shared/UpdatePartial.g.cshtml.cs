#pragma checksum "C:\Projects\Tribat\sourcecode\tribatvn\Views\Shared\UpdatePartial.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "d426a7561c7ae79ec0cef9920e7e4fdfa4c37f7f"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Shared_UpdatePartial), @"mvc.1.0.view", @"/Views/Shared/UpdatePartial.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/Shared/UpdatePartial.cshtml", typeof(AspNetCore.Views_Shared_UpdatePartial))]
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
#line 1 "C:\Projects\Tribat\sourcecode\tribatvn\Views\_ViewImports.cshtml"
using Models;

#line default
#line hidden
#line 1 "C:\Projects\Tribat\sourcecode\tribatvn\Views\Shared\UpdatePartial.cshtml"
using Helpers;

#line default
#line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"d426a7561c7ae79ec0cef9920e7e4fdfa4c37f7f", @"/Views/Shared/UpdatePartial.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"55f7d09cb6699920b8416cd86872bb94362cdab7", @"/Views/_ViewImports.cshtml")]
    public class Views_Shared_UpdatePartial : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("alt", new global::Microsoft.AspNetCore.Html.HtmlString("Image"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("class", new global::Microsoft.AspNetCore.Html.HtmlString("img-fluid w-100"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("src", new global::Microsoft.AspNetCore.Html.HtmlString("~/assets/wingman/img/graphic-woman-writing.svg"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
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
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#line 2 "C:\Projects\Tribat\sourcecode\tribatvn\Views\Shared\UpdatePartial.cshtml"
  
    var language = ViewData["Language"].ToString();

#line default
#line hidden
            BeginContext(77, 316, true);
            WriteLiteral(@"
<div class=""main-container"">
    <section class=""space-lg bg-white"">
        <div class=""container"">
            <div class=""row align-items-center justify-content-around"">
                <div class=""col-12 col-md-6 col-lg-5 text-center text-md-left section-intro"">
                    <h1 class=""display-3"">");
            EndContext();
            BeginContext(394, 23, false);
#line 11 "C:\Projects\Tribat\sourcecode\tribatvn\Views\Shared\UpdatePartial.cshtml"
                                     Write(Html.Raw(ViewBag.Title));

#line default
#line hidden
            EndContext();
            BeginContext(417, 72, true);
            WriteLiteral("</h1>\r\n                    <span class=\"lead\">\r\n                        ");
            EndContext();
            BeginContext(490, 43, false);
#line 13 "C:\Projects\Tribat\sourcecode\tribatvn\Views\Shared\UpdatePartial.cshtml"
                   Write(Html.Raw(TextHelper.GetText(541, language)));

#line default
#line hidden
            EndContext();
            BeginContext(533, 94, true);
            WriteLiteral("\r\n                    </span>\r\n                    <a href=\"/\" class=\"btn btn-success btn-lg\">");
            EndContext();
            BeginContext(628, 43, false);
#line 15 "C:\Projects\Tribat\sourcecode\tribatvn\Views\Shared\UpdatePartial.cshtml"
                                                          Write(Html.Raw(TextHelper.GetText(525, language)));

#line default
#line hidden
            EndContext();
            BeginContext(671, 141, true);
            WriteLiteral("</a>\r\n                </div>\r\n                <!--end of col-->\r\n                <div class=\"col-12 col-md-6 col-lg-6\">\r\n                    ");
            EndContext();
            BeginContext(812, 94, false);
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("img", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagOnly, "1e3f2da37fd04d97b8a79a69866927fd", async() => {
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_2);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            EndContext();
            BeginContext(906, 183, true);
            WriteLiteral("\r\n                </div>\r\n                <!--end of col-->\r\n            </div>\r\n            <!--end of row-->\r\n        </div>\r\n        <!--end of container-->\r\n    </section>\r\n</div>");
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
