define('reports',
    ['navigation', 'router', 'messanger', 'textprovider', 'presenter', 'dataservice', 'usercontext', 'vm.reports'],
    function (navigation, router, msgr, tp, presenter, dataservice, usercontext, vmReports) {

        window.onFrameResize = function (h) {
            //console.log(h);
            $('article').eq(0).find('iframe.ssrs-report-frame').height(h + 20);
        };

        var root = '';
        if (!usercontext.isCustomer()) {
            router.add('#/reports/:service/:report', function () {
                var vm = {
                    Url: [root, 'Report', this.service, this.report].join('/'),
                    template: 'Report_View'
                };
                presenter.show(vm);
            });

            if (usercontext.isAdmin()) {
                router.add('#/reports/admin/', function () {
                    vmReports.bind();
                    presenter.show(vmReports);
                });

                navigation.add(tp.get('nav_reports_admin'), '#/reports/admin/');
            }
        }

        return {
            getByModule: function (moduleId) {
                return $.Deferred(function (def) {
                    $.ajax({
                        type: 'POST',
                        url: root + 'Reports',
                        data: { moduleId: moduleId },
                        dataType: 'json',
                        success: function (res) {
                            //console.log(entities);
                            var result = [];
                            $(res.reports).each(function () {
                                var textResKey = ['report', this].join('_').toLowerCase().replace(/\W+/g, "_");
                                result.push({
                                    label: tp.get(textResKey),
                                    name: this,
                                    url: ['/#/reports', res.service, this].join('/')
                                });
                            });
                            def.resolve(result);
                        }
                    });
                }).promise();
            }
        };
    });