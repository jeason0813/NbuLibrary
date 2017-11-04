/// <reference path="../lib/ckeditor/ckeditor.js" />
define('templates',
    ['jquery', 'textprovider', 'navigation', 'router', 'messanger', 'presenter', 'usercontext'],
    function ($, tp, nav, router, msgr, presenter, usercontext) {
        if (usercontext.isAdmin()) {
            var vm = (function () {
                var _loaded = ko.observable(false);
                var _templates = ko.observableArray();
                var _activeItem = ko.observable();
                function load() {
                    _loaded(false);
                    _templates.removeAll();
                    $.when($.getJSON('/api/Templates/GetAllTemplates')).done(function (templates) {
                        ko.utils.arrayForEach(templates, function (item) {
                            _templates.push(item);
                        });
                        _loaded(true);
                    });
                }

                function edit(item) {
                    $.when($.getJSON('/api/Templates/GetTemplate', { id: item.Id })).done(function (template) {
                        template.template = "Admin_Templates_Edit";
                        var popup = presenter.popup(template);
                        popup.ok(tp.get('btn_save'), function () {
                            template.BodyTemplate = CKEDITOR.instances.bodyTemplate.getData();
                            template.SubjectTemplate = popup.getForm().find('#subjectTemplate').val();
                            $.when($.post('/api/Templates/Save', template)).done(function (result) {
                                if (result.success) {
                                    popup.close();
                                    msgr.success(tp.get('msg_saved_ok'));
                                }
                                else
                                    msg.error(result.error);
                            });
                        });
                        popup.show();
                    });
                }

                return {
                    template: 'Admin_Templates',
                    Loaded: _loaded,
                    Templates: _templates,
                    load: load,
                    edit: edit
                };
            })();

            nav.add(tp.get('nav_templates'), '#/templates/');
            router.add('#/templates/', function () {
                vm.load();
                presenter.show(vm);
            });
        }
    });