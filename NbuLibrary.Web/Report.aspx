<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Report.aspx.cs" Inherits="NbuLibrary.Web.Report" %>

<%@ Register Assembly="Microsoft.ReportViewer.WebForms, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91" Namespace="Microsoft.Reporting.WebForms" TagPrefix="rsweb" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script type="text/javascript" src="https://ajax.googleapis.com/ajax/libs/jquery/1.6.4/jquery.min.js"></script>
    <script>
        function resizeReportWrapper() {
            if (!$find('ReportViewer1').get_isLoading()) {
                var $visible = $('div[id^="VisibleReportContent"]');
                $visible.parent().css({ overflow: '', height: '' });

                var rptHeight, $iframe, $wrapper, $control;

                $iframe = $(parent.document.getElementById(window.name));
                $wrapper = $iframe.parent();
                $control = $('#ReportViewer1_fixedTable');
                if (!$.browser.msie) {
                    $control.children().first().children().each(function () {
                        $(this).height('0');
                    });

                    $('#ReportViewer1_ctl09').add('#ReportViewer1_ctl10').parent().height('fixed');
                }

                rptHeight = $control.outerHeight();
                if ($.browser.msie && parseInt($.browser.version, 10) < 9) {
                    rptHeight += 30;
                }
                if (window.hasWrapperResized === false && $wrapper.is('.frame-wrapper'))
                    rptHeight += $wrapper.outerHeight();

                //$wrapper.find('.reportLoading').filter(':visible').hide();
                $iframe.height(rptHeight);

                window.hasWrapperResized = true;
                clearInterval(window.resizeWrapperIntervalId);
                window.resizeWrapperIntervalId = undefined;

                if (parent && parent.onFrameResize)
                    parent.onFrameResize(rptHeight);
            }
        }
        window.hasWrapperResized = false;
        function pageLoad() {
            if (window.resizeWrapperIntervalId === undefined) {
                window.resizeWrapperIntervalId = setInterval(resizeReportWrapper, 100);
            }
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <rsweb:ReportViewer ID="ReportViewer1" runat="server" Font-Names="Verdana" Font-Size="8pt" ProcessingMode="Remote" WaitMessageFont-Names="Verdana" WaitMessageFont-Size="14pt" Width="100%">
        </rsweb:ReportViewer>

    </form>
</body>
</html>
