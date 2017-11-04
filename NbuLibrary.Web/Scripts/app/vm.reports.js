define('vm.reports',
	['ko', 'reports', 'presenter', 'messanger', 'textprovider'],
	function (ko, reportSvc, presenter, msgr, tp) {

	    var _loaded = ko.observable(false);
	    var _services = ko.observableArray();

	    function load() {
	        return $.Deferred(function (def) {
	            $.ajax({
	                type: 'POST',
	                url: '/Reports/All',
	                dataType: 'json',
	                success: function (services) {
	                    //console.log(entities);
	                    var result = [];
	                    $(services).each(function () {
	                        var textResKey = ['service', this.Name].join('_').toLowerCase().replace(/\W+/g, "_");
	                        for (var x in this.Reports) {
	                            this.Reports[x].Url = ['#/reports', this.Name, this.Reports[x].Name].join('/');
	                            this.Reports[x].Service = this.Name;
	                        }

	                        result.push({
	                            Label: tp.get(textResKey),
	                            Name: this.Name,
	                            Exists: ko.observable(this.Exists),
	                            Reports: ko.observableArray(this.Reports)
	                        });
	                    });
	                    def.resolve(result);
	                }
	            });
	        }).promise();
	    }
	    2
	    function createFolder(service) {

	        return $.Deferred(function (def) {
	            $.ajax({
	                type: 'POST',
	                url: '/Reports/Folder',
	                data: { service: service.Name },
	                dataType: 'json',
	                success: function (response) {
	                    //console.log(entities);
	                    if (response.ok) {
	                        service.Exists(true);
	                        msgr.success(tp.get('msg_reports_folder_created'));
	                    }
	                    def.resolve(response);
	                }
	            });
	        }).promise();
	    }

	    function uploadReport(service) {
	        var vm = {
	            Service: service.Name,
	            submitFunc: null,
	            validateFile: function (file) {
	                if (file.name.toLowerCase().indexOf('.rdl') != file.name.length - 4) {
	                    msgr.error(tp.get('msg_file_not_rdl'));
	                    return false;
	                }

	                if (file.size > 10485760) {
	                    msgr.error(tp.get('msg_file_to_big'));
	                    return false;
	                }

	                var form = popup.getForm();
	                if (!form.find('input[name="report_name"]').val()) {
	                    var fileNameWithoutExt = file.name.substring(0, file.name.length - 4);
	                    form.find('input[name="report_name"]').val(fileNameWithoutExt);
	                }

	                return true;
	            },
	            template: 'Report_Upload'
	        };
	        var popup = presenter.popup(vm, { title: tp.get('reports_upload_title') });
	        popup.ok(tp.get('btn_save'), function () {
	            var form = popup.getForm();
	            if (!form.find('input[name="report_name"]').val() || !vm.submitFunc)
	                return msgr.error(tp.get('msg_reports_upload_form_invalid'));
	            vm.submitFunc(function (result) {
	                msgr.success(tp.get('msg_reports_uploaded'));
	                popup.close();
	                $.when(load()).done(function (reloadedList) {
	                    $(reloadedList).each(function () {
	                        if (this.Name == service.Name) {
	                            service.Reports(this.Reports());
	                        }
	                    });
	                });
	            });
	        });
	        popup.show();
	    }

	    function callDelete(report) {
	        return $.ajax({
	            type: 'POST',
	            url: '/Reports/Delete',
	            data: { service: report.Service, report: report.Name },
	            dataType: 'json'
	        });
	    }

	    function removeReport(report) {
	        var serviceName = report.Service;
	        var service = null;
	        $(_services()).each(function () {
	            if (this.Name == serviceName)
	                service = this;
	        });

	        var ask = presenter.ask(tp.get('confirm_entity_deletion'));
	        ask.yes(function () {
	            $.when(callDelete(report))
                   .done(function (response) {
                       if (response.ok) {
                           msgr.success(tp.get('msg_saved_ok'));
                           ask.close();
                           $.when(load()).done(function (reloadedList) {
                               $(reloadedList).each(function () {
                                   if (this.Name == service.Name) {
                                       service.Reports(this.Reports());
                                   }
                               });
                           });
                       }
                       else
                           msgr.error(tp.get('msg_fail'));
                   });
	        });
	        ask.show();
	    }

	    function bind() {
	        _loaded(false);
	        $.when(load()).done(function (services) {
	            _services(services);
	            _loaded(true);
	        })
	    }

	    return {
	        bind: bind,
	        remove: removeReport,
	        createFolder: createFolder,
	        uploadReport: uploadReport,
	        Loaded: _loaded,
	        Services: _services,
	        template: 'Report_Admin'
	    };
	});