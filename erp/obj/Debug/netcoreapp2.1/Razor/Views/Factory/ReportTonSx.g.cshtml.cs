#pragma checksum "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "bad668b803f83f3b29eb222603e92e5a01465447"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Factory_ReportTonSx), @"mvc.1.0.view", @"/Views/Factory/ReportTonSx.cshtml")]
[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@"/Views/Factory/ReportTonSx.cshtml", typeof(AspNetCore.Views_Factory_ReportTonSx))]
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
#line 1 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
using Common.Utilities;

#line default
#line hidden
#line 2 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
using ViewModels;

#line default
#line hidden
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"bad668b803f83f3b29eb222603e92e5a01465447", @"/Views/Factory/ReportTonSx.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"55f7d09cb6699920b8416cd86872bb94362cdab7", @"/Views/_ViewImports.cshtml")]
    public class Views_Factory_ReportTonSx : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<TonSxViewModel>
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
            BeginContext(70, 2, true);
            WriteLiteral("\r\n");
            EndContext();
#line 5 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
  
    ViewData["Title"] = "Báo cáo tồn sản xuất";
    Layout = "~/Views/Shared/_LayoutData.cshtml";

#line default
#line hidden
            BeginContext(179, 2, true);
            WriteLiteral("\r\n");
            EndContext();
            BeginContext(181, 2147, false);
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("form", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "62337a7b99f94ed7968c04c67cce32ac", async() => {
                BeginContext(289, 130, true);
                WriteLiteral("\r\n    <div class=\"form-row mb-3\">\r\n        <div class=\"col-md-3 date-area\">\r\n            <label class=\"control-label\">Từ</label>\r\n");
                EndContext();
#line 14 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
              
                if (Model.from.HasValue)
                {

#line default
#line hidden
                BeginContext(496, 88, true);
                WriteLiteral("                    <input class=\"form-control form-control-lg datepicker datepicker-lg\"");
                EndContext();
                BeginWriteAttribute("value", " value=\"", 584, "\"", 632, 1);
#line 17 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
WriteAttributeValue("", 592, Model.from.Value.ToString("dd/MM/yyyy"), 592, 40, false);

#line default
#line hidden
                EndWriteAttribute();
                BeginContext(633, 5, true);
                WriteLiteral(" />\r\n");
                EndContext();
#line 18 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
                }
                else
                {

#line default
#line hidden
                BeginContext(698, 93, true);
                WriteLiteral("                    <input class=\"form-control form-control-lg datepicker datepicker-lg\" />\r\n");
                EndContext();
#line 22 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
                }
            

#line default
#line hidden
                BeginContext(825, 12, true);
                WriteLiteral("            ");
                EndContext();
                BeginContext(837, 61, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "0a21d3cb38654559969360f6e5c3c12a", async() => {
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper);
                __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper.InputTypeName = (string)__tagHelperAttribute_0.Value;
                __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_0);
#line 24 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
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
                BeginContext(898, 114, true);
                WriteLiteral("\r\n        </div>\r\n        <div class=\"col-md-3 date-area\">\r\n            <label class=\"control-label\">Đến</label>\r\n");
                EndContext();
#line 28 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
              
                if (Model.to.HasValue)
                {

#line default
#line hidden
                BeginContext(1087, 88, true);
                WriteLiteral("                    <input class=\"form-control form-control-lg datepicker datepicker-lg\"");
                EndContext();
                BeginWriteAttribute("value", " value=\"", 1175, "\"", 1221, 1);
#line 31 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
WriteAttributeValue("", 1183, Model.to.Value.ToString("dd/MM/yyyy"), 1183, 38, false);

#line default
#line hidden
                EndWriteAttribute();
                BeginContext(1222, 5, true);
                WriteLiteral(" />\r\n");
                EndContext();
#line 32 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
                }
                else
                {

#line default
#line hidden
                BeginContext(1287, 93, true);
                WriteLiteral("                    <input class=\"form-control form-control-lg datepicker datepicker-lg\" />\r\n");
                EndContext();
#line 36 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
                }
            

#line default
#line hidden
                BeginContext(1414, 12, true);
                WriteLiteral("            ");
                EndContext();
                BeginContext(1426, 59, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("input", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.SelfClosing, "a9859e20b6484197b4974c5b33a40a9f", async() => {
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.InputTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper);
                __Microsoft_AspNetCore_Mvc_TagHelpers_InputTagHelper.InputTypeName = (string)__tagHelperAttribute_0.Value;
                __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_0);
#line 38 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
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
                BeginContext(1485, 127, true);
                WriteLiteral("\r\n        </div>\r\n        <div class=\"col-md-6\">\r\n            <label class=\"control-label\">Tên NVL/BTP/TP</label>\r\n            ");
                EndContext();
                BeginContext(1612, 372, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("select", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "a10f51fe9d4b4a09b2c6610d57e5dfb9", async() => {
                    BeginContext(1695, 18, true);
                    WriteLiteral("\r\n                ");
                    EndContext();
                    BeginContext(1713, 32, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "e3f8e850a22c41b098a8e5a88dbbda94", async() => {
                        BeginContext(1730, 6, true);
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
                    BeginContext(1745, 2, true);
                    WriteLiteral("\r\n");
                    EndContext();
#line 44 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
                  
                    foreach (var item in Model.Products)
                    {

#line default
#line hidden
                    BeginContext(1848, 24, true);
                    WriteLiteral("                        ");
                    EndContext();
                    BeginContext(1872, 47, false);
                    __tagHelperExecutionContext = __tagHelperScopeManager.Begin("option", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "6867099f55ff4756b420efe24d207410", async() => {
                        BeginContext(1901, 9, false);
#line 47 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
                                               Write(item.Name);

#line default
#line hidden
                        EndContext();
                    }
                    );
                    __Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.OptionTagHelper>();
                    __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_OptionTagHelper);
                    BeginWriteTagHelperAttribute();
#line 47 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
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
                    BeginContext(1919, 2, true);
                    WriteLiteral("\r\n");
                    EndContext();
#line 48 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
                    }
                

#line default
#line hidden
                    BeginContext(1963, 12, true);
                    WriteLiteral("            ");
                    EndContext();
                }
                );
                __Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.SelectTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_SelectTagHelper);
#line 42 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
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
                BeginContext(1984, 337, true);
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
            AddHtmlAttributeValue("", 208, "/", 208, 1, true);
#line 10 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
AddHtmlAttributeValue("", 209, Constants.LinkFactory.Main, 209, 27, false);

#line default
#line hidden
            AddHtmlAttributeValue("", 236, "/", 236, 1, true);
#line 10 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
AddHtmlAttributeValue("", 237, Constants.LinkFactory.ReportTonSx, 237, 34, false);

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
            BeginContext(2328, 836, true);
            WriteLiteral(@"

<div class=""table-responsive"">
    <table class=""table table-sm table-bordered table-striped table-hover"">
        <thead>
            <tr>
                <th>
                    Tên NVL/BTP/TP
                </th>
                <th>
                    ĐVT
                </th>
                <th>
                    Tồn đầu ngày
                </th>
                <th>
                    Nhập từ kho
                </th>
                <th>
                    Xuất cho kho
                </th>
                <th>
                    Nhập từ sản xuất
                </th>
                <th>
                    Xuất cho sản xuất
                </th>
                <th>
                    Tồn cuối ngày
                </th>
            </tr>
        </thead>
        <tbody>
");
            EndContext();
#line 92 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
              
                var groups = (from p in Model.List
                              group p by new
                              {
                                  p.ProductId,
                                  p.Product,
                                  p.Unit
                              }
                              into d
                              select new
                              {
                                  productId = d.Key.ProductId,
                                  product = d.Key.Product,
                                  unit = d.Key.Unit,
                                  reports = d.ToList(),
                              }).ToList();

                foreach (var group in groups)
                {

#line default
#line hidden
            BeginContext(3940, 132, true);
            WriteLiteral("            <tr>\r\n                <td>\r\n                    <a href=\"#\" data-toggle=\"modal\" data-target=\"#chartTonSX\" data-product=\"");
            EndContext();
            BeginContext(4073, 13, false);
#line 113 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
                                                                                       Write(group.product);

#line default
#line hidden
            EndContext();
            BeginContext(4086, 134, true);
            WriteLiteral("\" data-source=\"70,13,20,90,44,12,30,30,30,10,5,0\" data-target-source=\"34\">\r\n                        <span class=\"icon-flickr\"></span> ");
            EndContext();
            BeginContext(4221, 13, false);
#line 114 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
                                                     Write(group.product);

#line default
#line hidden
            EndContext();
            BeginContext(4234, 93, true);
            WriteLiteral("\r\n                    </a>\r\n                </td>\r\n                <td>\r\n                    ");
            EndContext();
            BeginContext(4328, 10, false);
#line 118 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
               Write(group.unit);

#line default
#line hidden
            EndContext();
            BeginContext(4338, 67, true);
            WriteLiteral("\r\n                </td>\r\n                <td>\r\n                    ");
            EndContext();
            BeginContext(4406, 71, false);
#line 121 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
               Write(String.Format("{0:#,###,###.##}", group.reports.Sum(x => x.TonDauNgay)));

#line default
#line hidden
            EndContext();
            BeginContext(4477, 67, true);
            WriteLiteral("\r\n                </td>\r\n                <td>\r\n                    ");
            EndContext();
            BeginContext(4545, 70, false);
#line 124 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
               Write(String.Format("{0:#,###,###.##}", group.reports.Sum(x => x.NhapTuKho)));

#line default
#line hidden
            EndContext();
            BeginContext(4615, 67, true);
            WriteLiteral("\r\n                </td>\r\n                <td>\r\n                    ");
            EndContext();
            BeginContext(4683, 71, false);
#line 127 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
               Write(String.Format("{0:#,###,###.##}", group.reports.Sum(x => x.XuatChoKho)));

#line default
#line hidden
            EndContext();
            BeginContext(4754, 67, true);
            WriteLiteral("\r\n                </td>\r\n                <td>\r\n                    ");
            EndContext();
            BeginContext(4822, 74, false);
#line 130 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
               Write(String.Format("{0:#,###,###.##}", group.reports.Sum(x => x.NhapTuSanXuat)));

#line default
#line hidden
            EndContext();
            BeginContext(4896, 67, true);
            WriteLiteral("\r\n                </td>\r\n                <td>\r\n                    ");
            EndContext();
            BeginContext(4964, 75, false);
#line 133 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
               Write(String.Format("{0:#,###,###.##}", group.reports.Sum(x => x.XuatChoSanXuat)));

#line default
#line hidden
            EndContext();
            BeginContext(5039, 67, true);
            WriteLiteral("\r\n                </td>\r\n                <td>\r\n                    ");
            EndContext();
            BeginContext(5107, 72, false);
#line 136 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
               Write(String.Format("{0:#,###,###.##}", group.reports.Sum(x => x.TonCuoiNgay)));

#line default
#line hidden
            EndContext();
            BeginContext(5179, 44, true);
            WriteLiteral("\r\n                </td>\r\n            </tr>\r\n");
            EndContext();
#line 139 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
                }
            

#line default
#line hidden
            BeginContext(5257, 992, true);
            WriteLiteral(@"        </tbody>
    </table>
</div>

<!-- Modal -->
<div class=""modal fade"" id=""chartTonSX"" tabindex=""-1"" role=""dialog"" aria-labelledby=""chartTonSXLabel"" aria-hidden=""true"">
    <div class=""modal-dialog"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <button type=""button"" class=""close"" data-dismiss=""modal"">
                    <span aria-hidden=""true"">&times;</span><span class=""sr-only"">Đóng</span>
                </button>
                <h4 class=""modal-title"" id=""chartModalLabel""></h4>
            </div>
            <div class=""modal-body"">
                <canvas id=""canvas"" width=""568"" height=""300""></canvas>
            </div>
            <div class=""modal-footer"">
                <button type=""button"" class=""btn btn-secondary"" data-dismiss=""modal"">Đóng</button>
                <button type=""button"" class=""btn btn-primary"">Sản phẩm tiếp</button>
            </div>
        </div>
    </div>
</div>



");
            EndContext();
            DefineSection("scripts", async() => {
                BeginContext(6267, 6, true);
                WriteLiteral("\r\n    ");
                EndContext();
                BeginContext(6273, 94, false);
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("script", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "cf08b75269214234b172e1aa98a9939d", async() => {
                }
                );
                __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
                __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_6);
                BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "src", 2, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                AddHtmlAttributeValue("", 6309, "~/js/factory-reporttonsx.js?", 6309, 28, true);
#line 169 "C:\Projects\Tribat\sourcecode\erp\Views\Factory\ReportTonSx.cshtml"
AddHtmlAttributeValue("", 6337, DateTime.Now.Ticks, 6337, 19, false);

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
                BeginContext(6367, 2, true);
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
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TonSxViewModel> Html { get; private set; }
    }
}
#pragma warning restore 1591
