#pragma checksum "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "360a9a4df24e6a4cdd7bc2322b8b89d6fd82f55d"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Factory_ReportBH), @"mvc.1.0.view", @"/Views/Factory/ReportBH.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/Factory/ReportBH.cshtml", typeof(AspNetCore.Views_Factory_ReportBH))]
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
#line 1 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
using Common.Utilities;

#line default
#line hidden
#line 2 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
using ViewModels;

#line default
#line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"360a9a4df24e6a4cdd7bc2322b8b89d6fd82f55d", @"/Views/Factory/ReportBH.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"55f7d09cb6699920b8416cd86872bb94362cdab7", @"/Views/_ViewImports.cshtml")]
    public class Views_Factory_ReportBH : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<VanHanhViewModel>
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("type", "hidden", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("class", new global::Microsoft.AspNetCore.Html.HtmlString("hidedatepicker"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_2 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("value", "", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_3 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("class", new global::Microsoft.AspNetCore.Html.HtmlString("form-control form-control-lg js-select2-basic-single"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_4 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("method", "get", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_5 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("id", new global::Microsoft.AspNetCore.Html.HtmlString("form-main"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_6 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("type", new global::Microsoft.AspNetCore.Html.HtmlString("text/javascript"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
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
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.SelectTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper;
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            BeginContext(72, 2, true);
            WriteLiteral("\r\n");
            EndContext();
#line 5 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
  
    ViewData["Title"] = "Báo cáo bốc hàng";
    Layout = "~/Views/Shared/_LayoutData.cshtml";

#line default
#line hidden
            BeginContext(177, 2, true);
            WriteLiteral("\r\n");
            EndContext();
            BeginContext(179, 2145, false);
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("form", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "ac6a688b5d1b4026a1ea4d3a035a3b4b", async() => {
                BeginContext(283, 130, true);
                WriteLiteral("\r\n    <div class=\"form-row mb-3\">\r\n        <div class=\"col-md-3 date-area\">\r\n            <label class=\"control-label\">Từ</label>\r\n");
                EndContext();
#line 14 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
              
                if (Model.from.HasValue)
                {

#line default
#line hidden
                BeginContext(490, 88, true);
                WriteLiteral("                    <input class=\"form-control form-control-lg datepicker datepicker-lg\"");
                EndContext();
                BeginWriteAttribute("value", " value=\"", 578, "\"", 626, 1);
#line 17 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
WriteAttributeValue("", 586, Model.from.Value.ToString("dd/MM/yyyy"), 586, 40, false);

#line default
#line hidden
                EndWriteAttribute();
                BeginContext(627, 5, true);
                WriteLiteral(" />\r\n");
                EndContext();
#line 18 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                }
                else
                {

#line default
#line hidden
                BeginContext(692, 93, true);
                WriteLiteral("                    <input class=\"form-control form-control-lg datepicker datepicker-lg\" />\r\n");
                EndContext();
#line 22 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                }
            

#line default
#line hidden
                BeginContext(819, 12, true);
                WriteLiteral("            ");
                EndContext();
                BeginContext(831, 61, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "9cc3a0656c094dd897d2dec442608fe7", async() => {
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper);
                __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper.InputTypeName = (string)__tagHelperAttribute_0.Value;
                __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_0);
#line 24 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
__Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.from);

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
                BeginContext(892, 114, true);
                WriteLiteral("\r\n        </div>\r\n        <div class=\"col-md-3 date-area\">\r\n            <label class=\"control-label\">Đến</label>\r\n");
                EndContext();
#line 28 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
              
                if (Model.to.HasValue)
                {

#line default
#line hidden
                BeginContext(1081, 88, true);
                WriteLiteral("                    <input class=\"form-control form-control-lg datepicker datepicker-lg\"");
                EndContext();
                BeginWriteAttribute("value", " value=\"", 1169, "\"", 1215, 1);
#line 31 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
WriteAttributeValue("", 1177, Model.to.Value.ToString("dd/MM/yyyy"), 1177, 38, false);

#line default
#line hidden
                EndWriteAttribute();
                BeginContext(1216, 5, true);
                WriteLiteral(" />\r\n");
                EndContext();
#line 32 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                }
                else
                {

#line default
#line hidden
                BeginContext(1281, 93, true);
                WriteLiteral("                    <input class=\"form-control form-control-lg datepicker datepicker-lg\" />\r\n");
                EndContext();
#line 36 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                }
            

#line default
#line hidden
                BeginContext(1408, 12, true);
                WriteLiteral("            ");
                EndContext();
                BeginContext(1420, 59, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "8de516b48c8a43fe8f9bf565dcd9a27e", async() => {
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper);
                __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper.InputTypeName = (string)__tagHelperAttribute_0.Value;
                __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_0);
#line 38 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
__Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.to);

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
                BeginContext(1479, 127, true);
                WriteLiteral("\r\n        </div>\r\n        <div class=\"col-md-6\">\r\n            <label class=\"control-label\">Tên NVL/BTP/TP</label>\r\n            ");
                EndContext();
                BeginContext(1606, 372, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("select", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "681a64cbafd04af39b904fa76967a46b", async() => {
                    BeginContext(1689, 18, true);
                    WriteLiteral("\r\n                ");
                    EndContext();
                    BeginContext(1707, 32, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "2261390770594265983dc33e79ceb023", async() => {
                        BeginContext(1724, 6, true);
                        WriteLiteral("Tất cả");
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
                    BeginContext(1739, 2, true);
                    WriteLiteral("\r\n");
                    EndContext();
#line 44 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                  
                    foreach (var item in Model.Products)
                    {

#line default
#line hidden
                    BeginContext(1842, 24, true);
                    WriteLiteral("                        ");
                    EndContext();
                    BeginContext(1866, 47, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "ed2579287b3c461993b4a4bc11791499", async() => {
                        BeginContext(1895, 9, false);
#line 47 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                                               Write(item.Name);

#line default
#line hidden
                        EndContext();
                    }
                    );
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
                    __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
                    BeginWriteTagHelperAttribute();
#line 47 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                           WriteLiteral(item.Alias);

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
                    BeginContext(1913, 2, true);
                    WriteLiteral("\r\n");
                    EndContext();
#line 48 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                    }
                

#line default
#line hidden
                    BeginContext(1957, 12, true);
                    WriteLiteral("            ");
                    EndContext();
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.SelectTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper);
#line 42 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
__Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper.For = ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.nvl);

#line default
#line hidden
                __tagHelperExecutionContext.AddTagHelperAttribute("asp-for", __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper.For, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_3);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Write(__tagHelperExecutionContext.Output);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                EndContext();
                BeginContext(1978, 339, true);
                WriteLiteral(@"
        </div>

    </div>
    <div class=""form-row mb-3"">
        <div class=""col-12"">
            <label class=""control-label""><small>Bấm nút hoặc Enter</small></label>
            <button class=""btn btn-lg btn-info form-control"" type=""submit""><i class=""icon-magnifying-glass""></i> Tìm kiếm</button>
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
            __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper.Method = (string)__tagHelperAttribute_4.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_4);
            BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "action", 4, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
            AddHtmlAttributeValue("", 206, "/", 206, 1, true);
#line 10 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
AddHtmlAttributeValue("", 207, Constants.LinkFactory.Main, 207, 27, false);

#line default
#line hidden
            AddHtmlAttributeValue("", 234, "/", 234, 1, true);
#line 10 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
AddHtmlAttributeValue("", 235, Constants.LinkFactory.ReportBH, 235, 31, false);

#line default
#line hidden
            EndAddHtmlAttributeValues(__tagHelperExecutionContext);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_5);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            EndContext();
            BeginContext(2324, 390, true);
            WriteLiteral(@"

<div class=""table-responsive"">
    <table class=""table table-sm table-bordered table-striped table-hover"">
        <thead>
            <tr>
                <th></th>
                <th>
                    Thành phẩm
                </th>
                <th>
                    Số lượng bốc hàng
                </th>
            </tr>
        </thead>
        <tbody>
");
            EndContext();
#line 76 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
              
                var groups = (from p in Model.List
                              group p by new
                              {
                                  p.NVLTP
                              }
                              into d
                              select new
                              {
                                  ThanhPham = d.Key.NVLTP,
                                  reports = d.ToList()
                              }).ToList();

                foreach (var group in groups)
                {

#line default
#line hidden
            BeginContext(3278, 119, true);
            WriteLiteral("                    <tr>\r\n                        <td></td>\r\n                        <td>\r\n                            ");
            EndContext();
            BeginContext(3398, 15, false);
#line 94 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                       Write(group.ThanhPham);

#line default
#line hidden
            EndContext();
            BeginContext(3413, 91, true);
            WriteLiteral("\r\n                        </td>\r\n                        <td>\r\n                            ");
            EndContext();
            BeginContext(3505, 75, false);
#line 97 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                       Write(String.Format("{0:#,###,###.##}", group.reports.Sum(x => x.SoLuongBocHang)));

#line default
#line hidden
            EndContext();
            BeginContext(3580, 60, true);
            WriteLiteral("\r\n                        </td>\r\n                    </tr>\r\n");
            EndContext();
#line 100 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
                }
            

#line default
#line hidden
            BeginContext(3674, 42, true);
            WriteLiteral("        </tbody>\r\n    </table>\r\n</div>\r\n\r\n");
            EndContext();
            DefineSection("scripts", async() => {
                BeginContext(3734, 6, true);
                WriteLiteral("\r\n    ");
                EndContext();
                BeginContext(3740, 91, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("script", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "8f659eea32b04444a46f2e3482236d95", async() => {
                }
                );
                __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_6);
                BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "src", 2, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                AddHtmlAttributeValue("", 3776, "~/js/factory-van-hanh.js?", 3776, 25, true);
#line 107 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportBH.cshtml"
AddHtmlAttributeValue("", 3801, DateTime.Now.Ticks, 3801, 19, false);

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
                BeginContext(3831, 2, true);
                WriteLiteral("\r\n");
                EndContext();
            }
            );
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<VanHanhViewModel> Html { get; private set; }
    }
}
#pragma warning restore 1591
