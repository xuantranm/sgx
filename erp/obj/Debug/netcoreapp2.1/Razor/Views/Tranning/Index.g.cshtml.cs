#pragma checksum "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "657073c38852161540dfbd19088246cbb78f0148"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Tranning_Index), @"mvc.1.0.view", @"/Views/Tranning/Index.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/Tranning/Index.cshtml", typeof(AspNetCore.Views_Tranning_Index))]
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
#line 1 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
using Common.Utilities;

#line default
#line hidden
#line 2 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
using ViewModels;

#line default
#line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"657073c38852161540dfbd19088246cbb78f0148", @"/Views/Tranning/Index.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"55f7d09cb6699920b8416cd86872bb94362cdab7", @"/Views/_ViewImports.cshtml")]
    public class Views_Tranning_Index : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<TrainningViewModel>
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("value", "", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("class", new global::Microsoft.AspNetCore.Html.HtmlString("form-control"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("value", "10", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_3 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("value", "20", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_4 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("value", "50", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_5 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("value", "100", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_6 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("value", "500", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_7 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("method", "get", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
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
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.RenderAtEndOfFormTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.SelectTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(74, 2, true);
            WriteLiteral("\r\n");
            EndContext();
#line 5 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
  
    ViewData["Title"] = "Đào tạo";
    Layout = "~/Views/Shared/_LayoutSocialFb.cshtml";

#line default
#line hidden
            BeginContext(174, 2, true);
            WriteLiteral("\r\n");
            EndContext();
            BeginContext(176, 1726, false);
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("form", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "4feca703a55c43bc904f8c84c48ec795", async() => {
                BeginContext(264, 134, true);
                WriteLiteral("\r\n    <div class=\"form-row mb-3\">\r\n        <div class=\"col-md-4\">\r\n            <label class=\"control-label\">Loại</label>\r\n            ");
                EndContext();
                BeginContext(398, 141, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("select", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "3c9b800f8f5a4a5c849f4a3fa88cdeec", async() => {
                    BeginContext(466, 18, true);
                    WriteLiteral("\r\n                ");
                    EndContext();
                    BeginContext(484, 32, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "81d13d84a9e94ea9be8a9900b79be94e", async() => {
                        BeginContext(501, 6, true);
                        WriteLiteral("Tất cả");
                        EndContext();
                    }
                    );
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
                    __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value = (string)__tagHelperAttribute_0.Value;
                    __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_0);
                    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    if (!__tagHelperExecutionContext.Output.IsContentModified)
                    {
                        await __tagHelperExecutionContext.SetOutputContentAsync();
                    }
                    Write(__tagHelperExecutionContext.Output);
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    EndContext();
                    BeginContext(516, 14, true);
                    WriteLiteral("\r\n            ");
                    EndContext();
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.SelectTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper);
#line 14 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
__Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.type);

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("asp-for", __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper.For, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
#line 14 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
__Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper.Items = Model.Types;

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("asp-items", __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper.Items, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Write(__tagHelperExecutionContext.Output);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                EndContext();
                BeginContext(539, 120, true);
                WriteLiteral("\r\n        </div>\r\n        <div class=\"col-md-8\">\r\n            <label class=\"control-label\">Từ khóa</label>\r\n            ");
                EndContext();
                BeginContext(659, 47, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "a4a4d5998cf04ff0a0adb1c8055ef41e", async() => {
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper);
#line 20 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
__Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.search);

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("asp-for", __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper.For, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Write(__tagHelperExecutionContext.Output);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                EndContext();
                BeginContext(706, 162, true);
                WriteLiteral("\r\n        </div>\r\n    </div>\r\n    <div class=\"form-row mb-3\">\r\n        <div class=\"col-md-2\">\r\n            <label class=\"control-label\">Dòng</label>\r\n            ");
                EndContext();
                BeginContext(868, 311, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("select", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "421bbb5571ef4abca677d3cd45f8e8c0", async() => {
                    BeginContext(912, 18, true);
                    WriteLiteral("\r\n                ");
                    EndContext();
                    BeginContext(930, 30, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "9fdbc6d6c8ff41d090fba416e40ce2cf", async() => {
                        BeginContext(949, 2, true);
                        WriteLiteral("10");
                        EndContext();
                    }
                    );
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
                    __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value = (string)__tagHelperAttribute_2.Value;
                    __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_2);
                    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    if (!__tagHelperExecutionContext.Output.IsContentModified)
                    {
                        await __tagHelperExecutionContext.SetOutputContentAsync();
                    }
                    Write(__tagHelperExecutionContext.Output);
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    EndContext();
                    BeginContext(960, 18, true);
                    WriteLiteral("\r\n                ");
                    EndContext();
                    BeginContext(978, 30, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "50f7992fec684a57a8d37246f72eeabf", async() => {
                        BeginContext(997, 2, true);
                        WriteLiteral("20");
                        EndContext();
                    }
                    );
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
                    __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value = (string)__tagHelperAttribute_3.Value;
                    __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_3);
                    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    if (!__tagHelperExecutionContext.Output.IsContentModified)
                    {
                        await __tagHelperExecutionContext.SetOutputContentAsync();
                    }
                    Write(__tagHelperExecutionContext.Output);
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    EndContext();
                    BeginContext(1008, 18, true);
                    WriteLiteral("\r\n                ");
                    EndContext();
                    BeginContext(1026, 30, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "be707cacd8fe4427b3d117a9eb427e8d", async() => {
                        BeginContext(1045, 2, true);
                        WriteLiteral("50");
                        EndContext();
                    }
                    );
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
                    __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value = (string)__tagHelperAttribute_4.Value;
                    __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_4);
                    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    if (!__tagHelperExecutionContext.Output.IsContentModified)
                    {
                        await __tagHelperExecutionContext.SetOutputContentAsync();
                    }
                    Write(__tagHelperExecutionContext.Output);
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    EndContext();
                    BeginContext(1056, 18, true);
                    WriteLiteral("\r\n                ");
                    EndContext();
                    BeginContext(1074, 32, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "156778491cba42d48407ffc1a59ad5b9", async() => {
                        BeginContext(1094, 3, true);
                        WriteLiteral("100");
                        EndContext();
                    }
                    );
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
                    __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value = (string)__tagHelperAttribute_5.Value;
                    __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_5);
                    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    if (!__tagHelperExecutionContext.Output.IsContentModified)
                    {
                        await __tagHelperExecutionContext.SetOutputContentAsync();
                    }
                    Write(__tagHelperExecutionContext.Output);
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    EndContext();
                    BeginContext(1106, 18, true);
                    WriteLiteral("\r\n                ");
                    EndContext();
                    BeginContext(1124, 32, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "10bbf06d46d9415a8e9ea3f542689d71", async() => {
                        BeginContext(1144, 3, true);
                        WriteLiteral("500");
                        EndContext();
                    }
                    );
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
                    __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value = (string)__tagHelperAttribute_6.Value;
                    __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_6);
                    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    if (!__tagHelperExecutionContext.Output.IsContentModified)
                    {
                        await __tagHelperExecutionContext.SetOutputContentAsync();
                    }
                    Write(__tagHelperExecutionContext.Output);
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    EndContext();
                    BeginContext(1156, 14, true);
                    WriteLiteral("\r\n            ");
                    EndContext();
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.SelectTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper);
#line 26 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
__Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.size);

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("asp-for", __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper.For, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Write(__tagHelperExecutionContext.Output);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                EndContext();
                BeginContext(1179, 118, true);
                WriteLiteral("\r\n        </div>\r\n        <div class=\"col-md-2\">\r\n            <label class=\"control-label\">Trang</label>\r\n            ");
                EndContext();
                BeginContext(1297, 308, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("select", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "646d418cb0414ae99cabb079d7ddfaad", async() => {
                    BeginContext(1341, 2, true);
                    WriteLiteral("\r\n");
                    EndContext();
#line 37 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
                  
                    var pages = Model.Pages;
                    for (var i = 1; i <= pages; i++)
                    {

#line default
#line hidden
                    BeginContext(1486, 24, true);
                    WriteLiteral("                        ");
                    EndContext();
                    BeginContext(1510, 30, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "1ecf81d8e61b410b9a05a3b53403a342", async() => {
                        BeginContext(1530, 1, false);
#line 41 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
                                      Write(i);

#line default
#line hidden
                        EndContext();
                    }
                    );
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
                    __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
                    BeginWriteTagHelperAttribute();
#line 41 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
                           WriteLiteral(i);

#line default
#line hidden
                    __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value = __tagHelperStringValueBuffer;
                    __tagHelperExecutionContext.AddTagHelperAttribute("value", __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper.Value, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                    await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                    if (!__tagHelperExecutionContext.Output.IsContentModified)
                    {
                        await __tagHelperExecutionContext.SetOutputContentAsync();
                    }
                    Write(__tagHelperExecutionContext.Output);
                    __tagHelperExecutionContext = __tagHelperScopeManager.End();
                    EndContext();
                    BeginContext(1540, 2, true);
                    WriteLiteral("\r\n");
                    EndContext();
#line 42 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
                    }
                

#line default
#line hidden
                    BeginContext(1584, 12, true);
                    WriteLiteral("            ");
                    EndContext();
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.SelectTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper);
#line 36 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
__Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.page);

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("asp-for", __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper.For, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Write(__tagHelperExecutionContext.Output);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                EndContext();
                BeginContext(1605, 290, true);
                WriteLiteral(@"
        </div>
        <div class=""col-md-8"">
            <label class=""control-label""><small>Bấm nút hoặc Enter</small></label>
            <button class=""btn btn-primary form-control"" type=""submit""><i class=""icon-magnifying-glass""></i> Tìm kiếm</button>
        </div>
    </div>
");
                EndContext();
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper);
            __Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.RenderAtEndOfFormTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper);
            __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper.Method = (string)__tagHelperAttribute_7.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_7);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "action", 4, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 203, "/", 203, 1, true);
#line 10 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
AddHtmlAttributeValue("", 204, Constants.LinkTraining.Main, 204, 28, false);

#line default
#line hidden
            AddHtmlAttributeValue("", 232, "/", 232, 1, true);
#line 10 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
AddHtmlAttributeValue("", 233, Constants.LinkTraining.Index, 233, 29, false);

#line default
#line hidden
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            EndContext();
            BeginContext(1902, 40, true);
            WriteLiteral("\r\n<small><span class=\"badge badge-info\">");
            EndContext();
            BeginContext(1943, 13, false);
#line 52 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
                                 Write(Model.Records);

#line default
#line hidden
            EndContext();
            BeginContext(1956, 157, true);
            WriteLiteral("</span> được tìm thấy.</small>\r\n<section class=\"flush-with-above\">\r\n    <div class=\"container\">\r\n        <ul class=\"row mb-3 feature-list feature-list-sm\">\r\n");
            EndContext();
#line 56 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
             foreach (var item in Model.List)
            {

#line default
#line hidden
            BeginContext(2175, 238, true);
            WriteLiteral("                <li class=\"col-12 col-md-6\">\r\n                    <div class=\"card card-lg\">\r\n                        <div class=\"embed-responsive embed-responsive-16by9\">\r\n                            <iframe class=\"embed-responsive-item\"");
            EndContext();
            BeginWriteAttribute("src", " src=\"", 2413, "\"", 2429, 1);
#line 61 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
WriteAttributeValue("", 2419, item.Link, 2419, 10, false);

#line default
#line hidden
            EndWriteAttribute();
            BeginContext(2430, 94, true);
            WriteLiteral(" allow=\"autoplay; encrypted-media\" allowfullscreen></iframe>\r\n                        </div>\r\n");
            EndContext();
            BeginContext(2738, 151, true);
            WriteLiteral("                        <div class=\"card-body\">\r\n                            <a href=\"#\">\r\n                                <h4 class=\"card-title mb-3\">");
            EndContext();
            BeginContext(2890, 9, false);
#line 68 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
                                                       Write(item.Name);

#line default
#line hidden
            EndContext();
            BeginContext(2899, 90, true);
            WriteLiteral("</h4>\r\n                            </a>\r\n                            <p class=\"card-text\">");
            EndContext();
            BeginContext(2990, 16, false);
#line 70 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
                                            Write(item.Description);

#line default
#line hidden
            EndContext();
            BeginContext(3006, 672, true);
            WriteLiteral(@"</p>
                        </div>
                        <div class=""card-footer card-footer-borderless d-none"">
                            <div class=""media blog-post-author"">
                                <img alt=""Image"" src=""assets/img/avatar-male-1.jpg"" class=""avatar avatar-xs"" />
                                <div class=""media-body"">
                                    <small>Cameron Vasquez</small>
                                    <small>28th February 2018 &bull; 4 min read</small>
                                </div>
                            </div>
                        </div>
                    </div>
                </li>
");
            EndContext();
#line 83 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
            }

#line default
#line hidden
            BeginContext(3693, 345, true);
            WriteLiteral(@"
        </ul>

        <!--PAGING-->
        <div class=""row justify-content-center d-none"">
            <div class=""col-auto"">
                <nav aria-label=""Page navigation example"">
                    <ul class=""pagination"">
                        <li class=""page-item disabled"">
                            <a class=""page-link""");
            EndContext();
            BeginWriteAttribute("href", " href=\"", 4038, "\"", 4074, 1);
#line 93 "C:\Projects\Tribat\sourcecode\erp\Views\Tranning\Index.cshtml"
WriteAttributeValue("", 4045, ViewContext.RouteData.Values, 4045, 29, false);

#line default
#line hidden
            EndWriteAttribute();
            BeginContext(4075, 1431, true);
            WriteLiteral(@" aria-label=""Trước"">
                                <span aria-hidden=""true"">
                                    <i class=""icon-chevron-left""></i>
                                </span>
                                <span class=""sr-only"">Trước</span>
                            </a>
                        </li>
                        <li class=""page-item active"">
                            <a class=""page-link"" href=""#"">1</a>
                        </li>
                        <li class=""page-item"">
                            <a class=""page-link"" href=""#"">2</a>
                        </li>
                        <li class=""page-item"">
                            <a class=""page-link"" href=""#"">3</a>
                        </li>
                        <li class=""page-item"">
                            <a class=""page-link"" href=""#"" aria-label=""Sau"">
                                <span aria-hidden=""true"">
                                    <i class=""icon-chevron-right""></i>
   ");
            WriteLiteral(@"                             </span>
                                <span class=""sr-only"">Sau</span>
                            </a>
                        </li>
                    </ul>
                </nav>
            </div>
            <!--end of col-->
        </div>
        <!--END PAGING-->
    </div>
</section>

<!--https://developers.google.com/youtube/v3/code_samples/dotnet-->");
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TrainningViewModel> Html { get; private set; }
    }
}
#pragma warning restore 1591
