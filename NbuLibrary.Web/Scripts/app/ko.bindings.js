/// <reference path="../lib/jQuery/jquery-1.9.1.js" />
define('ko.bindings',
    ['jquery', 'ko', 'entityservice', 'textprovider', 'dataservice', 'datacontext', 'messanger', 'textprovider', 'presenter', 'vm.filepicker'],
    function ($, ko, es, tp, dataservice, datacontext, msgr, tp, presenter, vmFilePicker) {

        //TODO: custom validator in ko.bindings!
        $.validator.addMethod("notnull", function (value, element, arg) {
            return !!value;
        }, $.validator.messages.required);
        $.validator.addMethod("daysafter", function (value, element, arg) {
            var v = new Date(value);
            var after = new Date();
            after.setDate(after.getDate() + parseInt(arg));
            return v > after;
        }, $.validator.format('Въведете дата поне {0} дни след днешната.'));
        $.validator.addMethod("daysbefore", function (value, element, arg) {
            var v = new Date(value);
            var today = new Date();
            v.setDate(v.getDate() + parseInt(arg));
            return v < today;
        }, $.validator.format('Въведете дата поне {0} дни преди днешната.'));
        $.validator.addMethod("regex",
                function (value, element, regexp) {
                    var re = regexp instanceof RegExp ? regexp : new RegExp(regexp);
                    return this.optional(element) || re.test(value);
                },
                "Невалиден формат."
        );
        $.validator.setDefaults({
            ignore: []
        });
        var ViewTypes = {
            Textfield: 0,
            Numberfield: 1,
            Datefield: 2,
            Enumfield: 3,
            Checkfield: 4,
            Listfield: 5,
            Filefield: 6,
            Htmlfield: 7
        };

        var EditTypes = {
            Textbox: 0,
            Numberbox: 1,
            Datepicker: 2,
            Enumlist: 3,
            Selectlist: 4,
            Checkbox: 5,
            FileUpload: 6,
            Autocomplete: 7,
            Htmlbox: 8
        };

        ko.showEntity = function (item, template) {
            var rs = template.indexOf('{');
            var ls = template.indexOf('}');
            var prevrs = -1;
            var prevls = -1;
            var res = [];
            while (rs >= 0) {
                res.push(template.substring(prevls + 1, rs));
                res.push(item.Data[template.substring(rs + 1, ls)]);
                prevls = ls;
                rs = template.indexOf('{', ls);
                ls = template.indexOf('}', ls + 1);
            }

            res.push(template.substring(prevls + 1, template.length));

            return res.join('');
        }
        ko.extractProp = function (template) {
            var rs = template.indexOf('{');
            var ls = template.indexOf('}');
            var prevrs = -1;
            var prevls = -1;
            var res = [];
            while (rs >= 0) {
                res.push(template.substring(rs + 1, ls));
                prevls = ls;
                rs = template.indexOf('{', ls);
                ls = template.indexOf('}', ls + 1);
            }

            return res;
        }

        ko.fileiconFor = function (filename, contentType) {
            function getExt(fn) {
                var idx = fn.lastIndexOf('.');
                if (idx > 0)
                    return fn.substring(idx + 1).toLowerCase();
                else
                    return 'unknown';
            };
            if (!contentType)
                return 'fileicon fileicon-' + getExt(filename);
            switch (contentType) {
                case 'image/png':
                    return 'fileicon fileicon-png';
                case 'image/jpg':
                case 'image/jpeg':
                    return 'fileicon fileicon-jpg';
                default:
                    return 'fileicon fileicon-' + getExt(filename);
            }
        }

        ko.stringToDate = function (s) {
            if (!s)
                return new Date();
            if (isNaN(new Date(s))) {
                var datePart = s.substring(0, s.indexOf('T'));
                var parts = datePart.split("-");
                var date = new Date(0);
                date.setYear(parseInt(parts[0]));
                date.setMonth(parseInt(parts[1]) - 1);
                if (parts[2][0] == '0') parts[2] = parts[2][1];
                date.setDate(parseInt(parts[2]));
                return date;
            }
            else
                return new Date(s);
        }

        ko.bindingHandlers.viewText = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
            },
            update: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {

                function getProp(fld, it) {
                    if (fld.Entity) {
                        var relData = it.RelationsData[fld.Entity + '_' + fld.Role];
                        if (!relData)
                            return null;

                        if (ko.isObservable(relData))
                            relData = relData();

                        if (!relData)
                            return null;
                        else
                            return relData.Entity.Data[fld.Property];
                    }
                    else {
                        var val = it.Data[fld.Property];
                        return val();
                    }
                }


                // This will be called once when the binding is first applied to an element,
                // and again whenever the associated observable changes value.
                // Update the DOM element based on the supplied values here.
                var field = valueAccessor().Field;
                var item = valueAccessor().Item;
                //console.log(valueAccessor());

                if (field.Type == ViewTypes.Listfield) {
                    var relItem = item.RelationsData[field.Entity + "_" + field.Role];
                    if (typeof (relItem) == "function")
                        relItem = relItem();
                    if (relItem && relItem.length) {
                        var ul = $('<ul></ul>').appendTo(element);
                        $(relItem).each(function () {
                            //TODO: we need more rebust way of including relational information
                            var li = $('<li></li>').appendTo(ul);
                            var stringValue = this.Entity.Data[field.Property];
                            if (stringValue === undefined) {
                                stringValue = this.Data[field.Property];
                                if (this.Entity.Data.Value) //e.g. this is a nomenclature
                                    stringValue += ' (' + this.Entity.Data.Value + ')';
                            }
                            $('<span></span>').text(stringValue).appendTo(li);
                        });
                    }
                }
                else if (field.Type == ViewTypes.Filefield) {
                    var relItem = item.RelationsData[field.Entity + "_" + field.Role];
                    if (typeof (relItem) == "function")
                        relItem = relItem();
                    if (relItem) {
                        if (field.Multiple && relItem.length) {
                            var ul = $('<ul></ul>').appendTo(element);
                            $(relItem).each(function () {
                                var li = $('<li></li>').appendTo(ul);
                                $('<a href="#/filedownload/' + this.Entity.Id + '"></a>').text(this.Entity.Data[field.Property]).appendTo(li);
                            });
                        }
                        else
                            $('<a href="#/filedownload/' + relItem.Entity.Id + '"></a>').text(relItem.Entity.Data[field.Property]).appendTo(element);
                    }
                }
                else if (field.Type == 2) { //datefield
                    var val = getProp(field, item);
                    if (val) {
                        var date = new Date(val);
                        $(element).text(date.toLocaleString());//TODO: format date
                    }
                    else
                        $(element).text('--');
                }
                else if (field.Type == 3) { // enumlist
                    $.when(es.getEnum(field.EnumClass))
                    .done(function (en) {
                        var val = getProp(field, item);
                        if (val !== null && val !== undefined)
                            $(element).text(tp.get('enum_' + en[val].toLowerCase()));
                    });

                }
                else if (field.Type === 4) {
                    if (!!getProp(field, item)) {
                        $('<i class="icon-ok"> </i>').appendTo(element);
                        $(element).find('.icon-remove').remove();
                    }

                    else {
                        $('<i class="icon-remove"> </i>').appendTo(element);
                        $(element).find('.icon-ok').remove();
                    }
                }
                else if (field.Type === 0 && field.Multiline) {
                    var text = getProp(field, item);
                    if (text && field.Length) {

                        var str = text.substring(0, field.Length);
                        if (str.length < text.length)
                            str += '...';
                        text = str;
                    }
                    $('<pre>' + (text || '--') + '</pre>').appendTo(element);
                }
                else if (field.Type === 0) { //textfield
                    var text = getProp(field, item);
                    if (text && field.Length) {

                        var str = text.substring(0, field.Length);
                        if (str.length < text.length)
                            str += '...';
                        text = str;
                    }
                    $(element).text(text || '--');
                }
                else if (field.Type == ViewTypes.Htmlfield) {
                    var text = getProp(field, item);
                    $(element).html(text || '--');
                }
                    //else if (field.Type == 1) {
                    //    if (field.Edit.IsSummary)
                    //        $(element).text(item[field.Property]().substring(0, 40) + "...");
                    //    else {
                    //        var pre = $(element).find('pre');
                    //        if (!pre.length)
                    //            pre = $('<pre />').appendTo(element);
                    //        pre.text(item[field.Property]());
                    //    }
                    //}
                    //else if (field.Type == 3) {
                    //    var date = NbuLib.Convert.stringToDate(item[field.Property]());
                    //    $(element).text(date.toLocaleDateString());
                    //}
                    //else if (field.Type == 5) {
                    //    var value = item[field.Property]();
                    //    $(element).text(field.Edit.Map[value]);
                    //}
                else {
                    var text = getProp(field, item);
                    $(element).text(text || '--');
                }
            }
        };
        ko.bindingHandlers.validate = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                $(element).validate({
                    errorClass: "alert alert-error",
                    highlight: function (element, errorClass) {
                        $(element).addClass('alert-error');
                    },
                    unhighlight: function (element, errorClass) {
                        $(element).removeClass('alert-error');
                    },
                    errorPlacement: function (error, element) {
                        if (element.data('errplace')) {
                            var errplace = element.parents(element.data('errplace'));
                            if (errplace.length) {
                                errplace.append(error);
                                return;
                            }
                        }
                        error.insertAfter(element);
                    }
                });
            }
        }

        ko.bindingHandlers.vClick = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                $(element).bind('click', function (e) {
                    e.preventDefault();
                    if ($(element).parents('form').valid())
                        valueAccessor().call(bindingContext.$data);
                });
            }
        }

        ko.bindingHandlers.editorFor = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                var field = valueAccessor().Field;
                var item = valueAccessor().Item;

                //console.log('editorFor', field);

                if (field.Type === EditTypes.Textbox && !field.Multiline) // textbox
                {
                    var input = $('<input type="text" placeholder="' + field.Label + '" ' + (item.Data[field.Property]() ? 'value="' + item.Data[field.Property]() + '"' : '') + ' />')
                        .attr({ id: field.Property, name: field.Property })
                        .appendTo(element);
                    if (field.MaxLength > 200)
                        input.addClass('input-xxlarge');
                    else
                        input.addClass('input-xlarge');

                    input.bind('keyup', function () {
                        item.Data[field.Property](input.val());
                        datacontext.set(field.Property, input.val());
                    });
                    if (field.Required)
                        $(input).rules('add', {
                            required: true
                        });
                    if (field.RegEx)
                        $(input).rules('add', {
                            regex: field.RegEx
                        });
                }
                else if (field.Type === EditTypes.Textbox && field.Multiline) // textbox
                {
                    var textarea = $('<textarea placeholder="' + field.Label + '">' + (item.Data[field.Property]() || "") + '</textarea>')
                        .attr({ id: field.Property, name: field.Property })
                        .appendTo(element)
                        .height(150);

                    if (field.MaxLength > 512)
                        textarea.addClass('input-xxlarge');
                    else if (field.MaxLength > 256)
                        textarea.addClass('input-xlarge');

                    textarea.bind('keyup', function () {
                        item.Data[field.Property](textarea.val());
                        datacontext.set(field.Property, textarea.val());
                    });
                    if (field.Required)
                        $(textarea).rules('add', {
                            required: true
                        });

                    if (field.MinLength)
                        $(textarea).rules('add', {
                            minlength: field.MinLength
                        });

                    if (field.MaxLength)
                        $(textarea).rules('add', {
                            maxlength: field.MaxLength
                        });
                }
                else if (field.Type == EditTypes.Numberbox) { //Number
                    var input = $('<input type="text" placeholder="' + field.Label + '" />')
                       .attr({ id: field.Property, name: field.Property })
                       .appendTo(element).addClass('input-small');

                    if (!!item.Data[field.Property]()) {
                        if (field.Integer)
                            input.val(parseInt(item.Data[field.Property]()));
                        else
                            input.val(parseFloat(item.Data[field.Property]()));
                    }
                    input.bind('keyup', function () {
                        item.Data[field.Property](input.val());
                        datacontext.set(field.Property, input.val());
                    });

                    $(input).rules('add', {
                        required: field.Required,
                        number: true,
                        integer: field.Integer
                    });

                    if (field.Min != null && field.Min != undefined) {
                        $(input).rules('add', { min: field.Min });
                    }
                    if (field.Max != null && field.Max != undefined) {
                        $(input).rules('add', { max: field.Max });
                    }
                }
                else if (field.Type == EditTypes.Datepicker) { //Date
                    var input = $('<input type="text" placeholder="' + field.Label + '" />').attr({ id: field.Property, name: field.Property });
                    input.appendTo(element)
                        .addClass('input-small')
                        .datepicker()
                        .bind('changeDate', function (e) {
                            var newVal = e.date.toISOString();
                            item.Data[field.Property](newVal);
                            datacontext.set(field.Property, newVal);
                        });
                    var date = item.Data[field.Property]() ? ko.stringToDate(item.Data[field.Property]()) : null;
                    if (date) input.datepicker('setValue', date);
                    if (field.Required)
                        $(input).rules('add', {
                            required: true
                        });

                    if (field.Future === true)
                        $(input).rules('add', {
                            daysafter: field.DaysOffset != null ? field.DaysOffset : 0
                        });
                    else if (field.Future === false)
                        $(input).rules('add', {
                            daysbefore: field.DaysOffset != null ? field.DaysOffset : 0
                        });
                }
                else if (field.Type == EditTypes.Enumlist) { // enumlist
                    var select = $('<select />').attr({ id: field.Property, name: field.Property });
                    if (!field.Multiple)
                        select.append($('<option></option>'));

                    $.when(es.getEnum(field.EnumClass))
                    .done(function (en) {
                        $.each(en, function (k, v) {
                            var opt = $('<option value="' + k + '">' + tp.get('enum_' + v.toLowerCase()) + '</option>').appendTo(select);
                            if (item.Data[field.Property]() == k)
                                opt.attr('selected', true);
                        });

                        select.appendTo(element).bind('change', function (e) {
                            item.Data[field.Property](select.val());
                            datacontext.set(field.Property, select.val());
                        });

                        select.select2({ width: 'resolve', placeholder: "Select", allowClear: true });

                        if (field.Required)
                            $(select).rules('add', {
                                notnull: true
                            });
                    });
                }
                else if (field.Type === EditTypes.Selectlist) { //selectlist
                    var vals = [];
                    var select = $('<select />').attr({ id: field.Property, name: field.Property });
                    if (!field.Multiple) select.append($('<option></option>'));
                    if (field.Multiple)
                        select.prop('multiple', true);
                    $.when(es.getEntity(field.Entity)).done(function (entity) {
                        var query = dataservice.search(field.Entity);
                        if (entity.IsNomenclature && ko.utils.arrayFirst(entity.Properties, function (p) { return p.Name === 'DisplayOrder' }) != null)
                            query.sort('DisplayOrder');
                        else
                            query.sort(ko.extractProp(field.Formula)[0]); //TODO: support multiple properties
                        ko.utils.arrayForEach(ko.extractProp(field.Formula), function (prop) { query.addProperty(prop); });
                        $.when(query.execute())
                        .done(function (data) {
                            $(data).each(function () {
                                var txt = ko.showEntity(this, field.Formula);
                                var optEl = $('<option value="' + this.Id + '">' + txt + '</option>').appendTo(select);
                                var currentItem = item.RelationsData[field.Entity + "_" + field.Role] ? item.RelationsData[field.Entity + "_" + field.Role]() : null;
                                if (!currentItem)
                                    return;


                                if (currentItem.length === 0)
                                    return;
                                if (!field.Multiple) {
                                    if (currentItem.Entity.Id == this.Id) {
                                        optEl.prop('selected', true);
                                        vals.push(this.Id);
                                    }
                                }
                                else {
                                    for (var x in currentItem) {
                                        if (this.Id == currentItem[x].Entity.Id) {
                                            optEl.prop('selected', true);
                                            //vals.push(this.Id);
                                            break;
                                        }
                                    }
                                }
                            });

                            select
                            .appendTo(element)
                            .select2({ width: 'resolve', placeholder: "Select", allowClear: true })
                            .bind('change', function (e) {

                                if (e.added) {
                                    datacontext.addRelation(field.Entity, field.Role, e.added.id)
                                }
                                else if (e.removed) {
                                    datacontext.removeRelation(field.Entity, field.Role, e.removed.id)
                                }
                                else {
                                    datacontext.addRelation(field.Entity, field.Role, parseInt(select.val()))
                                }

                                //item[field.Entity + "_" + field.Role] = entityItem;
                            });

                            if (field.Required)
                                $(select).rules('add', {
                                    notnull: true
                                });
                            //select.val(vals);
                        });
                    });
                }
                else if (field.Type == EditTypes.Autocomplete) {
                    var input = $('<input value="0" type="hidden"/>').attr({ id: field.Property, name: field.Property }).addClass('input-xlarge').appendTo(element);
                    var ajaxSearch = {
                        url: "api/entity/search",
                        type: 'post',
                        dataType: 'json',
                        data: function (term, page) {
                            var q = dataservice.search(field.Entity);
                            q.allProperties(true);//todo: all props
                            q.rules.startsWith(field.Property, term);
                            q.setPage(page, 10);
                            return q.getRequest();
                        },
                        results: function (data, page) {
                            var more = data.length == 10; // whether or not there are more results available

                            var res = [];
                            ko.utils.arrayForEach(data, function (item) {
                                res.push({ text: item.Data[field.Property], id: item.Id });
                            });
                            // notice we return the value of more so Select2 knows if more results can be loaded
                            return { results: res };
                        }
                    };

                    function initSelection(el, callback) {
                        var currentItem = item.RelationsData[field.Entity + "_" + field.Role] ? item.RelationsData[field.Entity + "_" + field.Role]() : null;
                        if (!currentItem || currentItem.length === 0) {
                            input.val('');
                            if (field.Multiple)
                                callback([]);
                            else
                                callback({});
                        }
                        else if (!field.Multiple) {
                            callback({ id: currentItem.Entity.Id, text: currentItem.Entity.Data[field.Property] });
                        }
                        else {
                            var selectedData = [];
                            for (var x in currentItem) {
                                selectedData.push({ id: currentItem[x].Entity.Id, text: currentItem[x].Entity.Data[field.Property] });
                            }
                            callback(selectedData);
                        }
                    };
                    input.select2({ multiple: field.Multiple, ajax: ajaxSearch, minimumInputLength: 2, width: 'resolve', allowClear: true, placeholder: "Select", initSelection: initSelection })
                    .bind('change', function (e) {

                        if (e.added) {
                            datacontext.addRelation(field.Entity, field.Role, e.added.id)
                        }
                        else if (e.removed) {
                            datacontext.removeRelation(field.Entity, field.Role, e.removed.id)
                        }
                        else {
                            datacontext.addRelation(field.Entity, field.Role, parseInt(select.val()))
                        }
                        //item[field.Entity + "_" + field.Role] = entityItem;
                    });

                    if (field.Required)
                        $(input).rules('add', {
                            notnull: true
                        });
                }
                else if (field.Type === EditTypes.Checkbox) { //checkbox
                    $('<input type="checkbox">')
                        .prop('checked', !!item.Data[field.Property]())
                        .appendTo(element).change(function (e) {
                            var val = $(this).prop('checked');
                            item.Data[field.Property](val);
                            datacontext.set(field.Property, val);

                        });
                }
                else if (field.Type == EditTypes.FileUpload) {
                    var $fu = $('<div class="fileupload">'
                        + '<table class="table table-bordered"><tr><td colspan="3" class="btn-group">'
                        + '<span class="btn btn-primary fileinput-button"><b class="icon-upload"></b>&nbsp;' + tp.get('btn_upload_file') + '<input id="fileupload" type="file" data-errplace=".fileupload" name="files[]"></span>'
                        + '<button class="btn btn-success add-existing"><b class="icon-plus"></b>&nbsp;' + tp.get('btn_add_existing_file') + '</button>'
                        + '</td></tr></table>'
                        + '</div>').appendTo(element);
                    //var $btn = $fu.find('.upload');
                    var $tbl = $fu.find('table');

                    function refreshUI() {
                        if (field.Multiple)
                            return;

                        var filesCnt = $tbl.find('tr').not('.error').length - 1;
                        if (filesCnt > 0) {
                            $tbl.find('.add-existing, #fileupload, .fileinput-button').each(function () {
                                $(this).attr('disabled', true);
                            });
                        }
                        else {
                            $tbl.find('.add-existing, #fileupload, .fileinput-button').each(function () {
                                $(this).attr('disabled', false);
                            });
                        }
                    }

                    function appendFile(file, removeElement) {
                        var tr = $(['<tr><td class="span1"><div class="', ko.fileiconFor(file.Data.Name + file.Data.Extension), '">&nbsp;</div></td><td><a href="#/filedownload/', file.Id, '">', file.Data.Name, '</a></td><td><button class="btn btn-small btn-danger remove"><i class="icon-trash">&nbsp;</i>&nbsp;Remove</button></td></tr>'].join(''))
                                .appendTo($tbl);
                        tr.on('click', '.remove', function (e) {
                            e.preventDefault();
                            tr.addClass('error');
                            datacontext.removeRelation(field.Entity, field.Role, file.Id);
                            $(this).attr('disabled', true);
                            if (removeElement)
                                setTimeout(function () { tr.remove(); }, 1000);
                            refreshUI();
                            //$.when(dataservice.remove('File', item.Entity.Id, true)).done(function () {
                            //    tr.remove();
                            //    datacontext.removeRelation(field.Entity, field.Role, item.Entity.Id);
                            //});
                        });
                        refreshUI();
                    }

                    $tbl.on('click', '.add-existing', function (e) {
                        e.preventDefault();
                        vmFilePicker.bind();
                        var popup = presenter.popup(vmFilePicker, { title: tp.get('heading_pick_existing_file') });

                        popup.ok(tp.get('btn_pick_file'), function () {
                            datacontext.addRelation(field.Entity, field.Role, vmFilePicker.Selected().Id);
                            appendFile(vmFilePicker.Selected(), true);
                            popup.close();
                        });

                        popup.show();
                    });
                    if (field.Required) {
                        $fu.find('input[type="file"]').rules('add', {
                            notnull: true
                        });
                    }
                    $fu.find('input[type="file"]').fileupload({
                        url: 'file/upload',
                        dataType: 'json',
                        add: function (e, data) {
                            var file = data.files[0];
                            if (file.size > 10485760)
                                msgr.error(tp.get('msg_file_to_big'));
                            else
                                data.submit();
                        },
                        send: function (e, data) {
                            var file = data.files[0];
                            var tr = $(['<tr><td class="span1"><div class="', ko.fileiconFor(file.name), '">&nbsp;</div></td><td>', file.name, '</td><td><div class="progress"><div class="bar" style="width: 0%;"></div></div></td></tr>'].join('')).appendTo($tbl);
                            tr.addClass('warning');
                        },
                        progress: function (e, data) {
                            var progress = parseInt(data.loaded / data.total * 100, 10);
                            var bar = $tbl.find('.progress .bar');
                            bar.css({ width: progress + '%' });
                        },
                        done: function (e, data) {
                            //$btn.prop('disabled', false);
                            var file = data.result.files && data.result.files.length ? data.result.files[0] : null;
                            var tr = $tbl.find('tr.warning').last();
                            tr.removeClass('warning');
                            if (!file) {
                                tr.addClass('error');
                                setTimeout(function () { tr.remove(); }, 2000);
                                if (data.result.error && data.result.error == 'Files of this type are not allowed.')
                                    msgr.error('msg_file_upload_failed_extension_not_allowed');
                                else if (data.result.error && data.result.error == 'Disk usage limit exceeded.')
                                    msgr.error('msg_file_upload_failed_disk_usage_limit_exceeded');
                                else
                                    msgr.error('msg_file_upload_failed');
                                return;
                            }
                            tr.addClass('success');
                            setTimeout(function () { tr.removeClass('success'); }, 1000);
                            var td = tr.find('td').last();
                            td.find('.progress').remove();
                            td.append('<button class="btn btn-small btn-danger remove"><i class="icon-trash">&nbsp;</i>&nbsp;Remove</button>');
                            datacontext.addRelation(field.Entity, field.Role, file.id);
                            tr.on('click', '.remove', function (e) {
                                e.preventDefault();
                                //TODO: actually remove the file
                                tr.addClass('error');
                                $.when(dataservice.remove('File', file.id, true)).done(function () {
                                    tr.remove();
                                    datacontext.removeRelation(field.Entity, field.Role, file.id);
                                    refreshUI();
                                });
                            });
                            var td2 = tr.find('td:nth-child(2)');
                            td2.html('');
                            td2.append(['<a href="', file.url, '">', file.name, '</a>'].join(''));
                            refreshUI();
                        },
                    });

                    var currentItems = item.RelationsData[field.Entity + '_' + field.Role];
                    if (currentItems && currentItems()) {
                        currentItems = currentItems();
                        if (field.Multiple)
                            ko.utils.arrayForEach(currentItems, function (item) {
                                appendFile(item.Entity);
                            });
                        else
                            appendFile(currentItems.Entity);
                    }

                }
                else if (field.Type == EditTypes.Htmlbox) {
                    var editor = $('<textarea id="bodyTemplate" style="min-height:150px" class="input-xxlarge">' + (item.Data[field.Property]() || "") + '</textarea>').appendTo(element).ckeditor();
                    editor.editor.on('change', function () {
                        var data = editor.editor.getData();
                        item.Data[field.Property](data);
                        datacontext.set(field.Property, data);
                    });
                }
                else {
                    var input = $('<input type="text" placeholder="' + field.Label + '" ' + (item.Data[field.Property]() ? 'value="' + item.Data[field.Property]() + '"' : '') + ' />')
                        .attr({ id: field.Property, name: field.Property })
                        .appendTo(element);

                    input.bind('keyup', function () {
                        item.Data[field.Property](input.val());
                        datacontext.set(field.Property, input.val());

                    });
                    if (field.Edit.Required)
                        $(input).rules('add', {
                            required: true
                        });
                }

            },
            update: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                var field = valueAccessor().Field;
                var item = valueAccessor().Item;
                switch (field.Type) {
                    case EditTypes.Textbox:
                    case EditTypes.Numberbox:
                        $(self.element).find('input').val(item.Data[field.Property]());
                        break;
                    case EditTypes.Selectlist:
                        if (field.Multiple) console.log('Multiple select update is not implemented:', field);
                        var select = $(element).find('select');
                        var currentItem = item.RelationsData[field.Entity + "_" + field.Role] ? item.RelationsData[field.Entity + "_" + field.Role]() : null;
                        if (!currentItem)
                            select.find('option').prop('selected', false);
                        else
                            select.find('option[value="' + currentItem.id + '"]').prop('selected', true);
                        break;
                }
            }
        };

        ko.bindingHandlers.filterFor = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                var field = valueAccessor();
                //console.log(field);


                var field = valueAccessor().Field;
                var currFilter = bindingContext.$root.getFilterBy(field);


                if (field.Entity) {
                    var edit = field.Edit;
                    var select = $('<select />').attr({ id: field.Property, name: field.Property });
                    client.search({ Entity: field.Entity }, function (data) {
                        select.append($('<option></option>'));
                        $(data).each(function () {
                            var optEl = $('<option value="' + this.Id + '">' + this[field.Property] + '</option>').appendTo(select);

                            if (currFilter && currFilter.PropertyRules[0].Values == this.Id)
                                optEl.attr('selected', true);

                            //var currentItem = item[field.Entity + "_" + field.Role];
                            //if (currentItem && currentItem[field.Property] == this[field.Property]) //TODO: Use IDs instead
                            //    optEl.attr('selected', true);
                        });
                    });
                    select.appendTo(element).bind('change', function (e) {
                        var selected = select.children(':selected');
                        //var entityItem = { Id: selected.val(), EntityName: field.Entity };
                        //entityItem[field.Property] = selected.text();
                        //item[field.Entity + "_" + field.Role] = entityItem;
                        bindingContext.$root.filterBy(field, selected.val());
                    });

                    if (field.Edit.Required)
                        $(select).rules('add', {
                            notnull: true
                        });
                }
                else if (field.Type == 1) {
                    //var textarea = $('<textarea type="text" placeholder="' + field.Label + '">' + item[field.Property]() + '</textarea>')
                    //    .attr({ id: field.Property, name: field.Property })
                    //    .appendTo(element);

                    //if (field.Edit.Required)
                    //    $(textarea).rules('add', {
                    //        required: true
                    //    });

                    //textarea.width(500);
                    //var maxRows = field.Edit.MaxLength / 50;

                    //textarea[0].rows = maxRows > 10 ? 10 : maxRows;

                    //textarea.bind('keyup', function () {
                    //    item[field.Property](textarea.val());
                    //});
                }
                else if (field.Type == 2) { //Number
                    //var input = $('<input type="text" placeholder="' + field.Label + '" />')
                    //   .attr({ id: field.Property, name: field.Property })
                    //   .appendTo(element);

                    //if (field.Edit.Integer)
                    //    input.val(parseInt(item[field.Property]()));
                    //else
                    //    input.val(parseFloat(item[field.Property]()));

                    //input.bind('keyup', function () {
                    //    item[field.Property](input.val());
                    //});

                    //$(input).rules('add', {
                    //    required: field.Edit.Required,
                    //    number: true,
                    //    integer: field.Edit.Integer
                    //});
                }
                else if (field.Type == 3) { //Date
                    //var input = $('<input type="text" placeholder="' + field.Label + '" />').attr({ id: field.Property, name: field.Property });
                    //input.appendTo(element)
                    //    .datepicker()
                    //    .bind('changeDate', function (e) {
                    //        item[field.Property](e.date.toISOString());
                    //    });
                    //var date = NbuLib.Convert.stringToDate(item[field.Property]());
                    //input.datepicker('setDate', date);
                    //if (field.Edit.Required)
                    //    $(input).rules('add', {
                    //        required: true
                    //    });
                }
                else if (field.Type == 5) {
                    var select = $('<select />').attr({ id: field.Property, name: field.Property });
                    select.append($('<option></option>'));
                    for (var val = 0; val < field.Edit.Map.length; val++) {
                        var opt = $('<option value="' + val + '">' + field.Edit.Map[val] + '</option>').appendTo(select);
                        if (currFilter && currFilter.Values == val)
                            opt.attr('selected', true);
                        //if (filterHolder && filterHolder[field.Property]() == val)
                        //opt.attr('selected', true);
                    }
                    select.appendTo(element).bind('change', function (e) {
                        //filterHolder[field.Property](select.val());
                        bindingContext.$root.filterBy(field, select.val());
                    });
                }
                else {
                    var value = "";
                    if (currFilter)
                        value = currFilter.Values;
                    var input = $('<input type="text" placeholder="' + field.Label + '" value="' + value + '" />')
                        .attr({ id: field.Property, name: field.Property })
                        .appendTo(element);

                    input.bind('keyup', function () {
                        //item[field.Property](input.val());
                        bindingContext.$root.filterBy(field, input.val());
                    });
                }

            },
            update: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                // This will be called once when the binding is first applied to an element,
                // and again whenever the associated observable changes value.
                // Update the DOM element based on the supplied values here.
            }
        };

        ko.bindingHandlers.actionFor = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                var act = valueAccessor().action;
                var item = valueAccessor().item;
                //console.log(act);

                var disabled = false;
                if (act.canExecute && !act.canExecute.call(bindingContext.$root, item)) {
                    disabled = true;
                    $(element).addClass('disabled');
                    $(element).parent().addClass('disabled');
                }
                if (!disabled)
                    $(element).click(function (e) {
                        e.preventDefault();
                        act.Callback.call(bindingContext.$root, item);
                    });
                if (act.Icon) {
                    var icon = $('<i class="icon ' + act.Icon + '"></i>');
                    $(element).text(' ' + act.Label).prepend(icon);
                }
                else
                    $(element).text(act.Label);
            },
            update: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                // This will be called once when the binding is first applied to an element,
                // and again whenever the associated observable changes value.
                // Update the DOM element based on the supplied values here.
            }
        };

        ko.bindingHandlers.res = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                $(element).text(tp.get(valueAccessor()));
            }
        }

        ko.bindingHandlers.select2 = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                var va = valueAccessor();
                var options = $.extend({ width: 'resolve', allowClear: true }, va.options || {});
                setTimeout(function () {
                    var sel = $(element).select2(options);
                    if (va.changeHandler)
                        sel.bind('change', va.changeHandler);
                    if (va.value && !sel.attr('multiple'))//TODO: ko.bindingHandlers.select2 - implement value opt for multiple select
                    {
                        sel.bind('change', function (e) {
                            if (e.added) {
                                if (ko.isObservable(va.value))
                                    va.value(e.added.id);
                            }
                            else if (e.removed) {
                                if (ko.isObservable(va.value))
                                    va.value(null);
                            }
                        })
                    }

                }, 25); //TODO: artificial timeout in order to handle selected item when using knockoutjs binding for the options
            }
        }

        ko.bindingHandlers.accordion = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                //TODO: collapse accordions in a better way
                if (valueAccessor().collapse) {
                    setTimeout(function () {
                        $(element).find('.accordion-body').collapse();
                    }, 50);
                }
            }
        }

        ko.bindingHandlers.enumlist = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {

                var defaults = { multiple: false, required: false };
                var va = valueAccessor();
                va = $.extend(defaults, va);
                var value = va.value;
                var observable = null;
                if (ko.isObservable(value)) {
                    observable = value;
                    value = value();
                }
                var enumClass = va.enumClass;

                //TODO: code repetition with editorFor
                var select = $('<select />');

                if (!va.multiple)
                    select.append($('<option></option>'));
                else
                    select.attr('multiple', true);

                if (va.stretch)
                    select.css('width', '100%');

                $.when(es.getEnum(enumClass))
                .done(function (en) {
                    $.each(en, function (k, v) {
                        var opt = $('<option value="' + k + '">' + tp.get('enum_' + v.toLowerCase()) + '</option>').appendTo(select);
                        if (!va.multiple && k == value)
                            opt.attr('selected', true);
                        else if (va.multiple && value && value.length && ko.utils.arrayFirst(value, function (x) { return x == k; }) != null)
                            opt.attr('selected', true);
                    });

                    select.appendTo(element).bind('change', function (e) {
                        if (observable)
                            observable(select.val());
                        else
                            value = select.val();
                    });

                    select.select2({ width: 'resolve', placeholder: "Select", allowClear: true });
                    if (va.disable)
                        select.select2('disable');

                    if (va.required)
                        $(select).rules('add', {
                            notnull: true
                        });
                });
            }
        }

        ko.bindingHandlers.datepicker = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {

                var defaults = { required: false };
                var va = valueAccessor();
                va = $.extend(defaults, va);
                var value = va.value;
                var observable = null;
                if (ko.isObservable(value)) {
                    observable = value;
                    value = value();
                }

                var input = $('<input type="text" />');
                input.appendTo(element)
                    .datepicker()
                    .bind('changeDate', function (e) {
                        var newVal = e.date.toISOString();
                        if (observable)
                            observable(newVal);
                    });
                var date = value ? ko.stringToDate(value) : null;
                if (date) input.datepicker('setValue', date);
                if (va.required)
                    $(input).rules('add', {
                        required: true
                    });
            }
        }

        ko.bindingHandlers.selectlist = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {

                var defaults = { multiple: false, required: false };
                var va = valueAccessor();
                va = $.extend(defaults, va);
                var value = va.value;
                var observable = null;
                if (ko.isObservable(value)) {
                    observable = value;
                    value = value();
                }

                var vals = [];
                var select = $('<select />');
                if (!va.multiple)
                    select.append($('<option></option>'));
                else
                    select.prop('multiple', true);

                if (va.id)
                    select.attr('id', va.id);
                if (va.name)
                    select.attr('name', va.name);

                if (va.stretch)
                    select.css('width', '100%');

                var query = dataservice.search(va.entity);
                query.allProperties(true); //TODO: Selectlist queries all properties!
                query.sort(ko.extractProp(va.formula)[0]);
                $.when(query.execute())
                .done(function (data) {
                    $(data).each(function () {
                        var txt = ko.showEntity(this, va.formula);
                        var optEl = $('<option value="' + this.Id + '">' + txt + '</option>').appendTo(select);
                        var currentItem = value;
                        if (!currentItem)
                            return;


                        if (currentItem.length === 0)
                            return;
                        if (!va.multiple) {
                            if (currentItem.Entity.Id == this.Id) {
                                optEl.prop('selected', true);
                                vals.push(this.Id);
                            }
                        }
                        else {
                            for (var x in currentItem) {
                                if (this.Id == currentItem[x].Entity.Id) {
                                    optEl.prop('selected', true);
                                    //vals.push(this.Id);
                                    break;
                                }
                            }
                        }
                    });

                    select
                    .appendTo(element)
                    .select2({ width: 'resolve', placeholder: "Select", allowClear: true })
                    .bind('change', va.changeHandler);

                    if (va.required) {
                        $(select).rules('add', {
                            notnull: true
                        });
                    }
                    //select.val(vals);
                });
            }
        }
        ko.bindingHandlers.validation = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                $(element).rules('add', valueAccessor());
            }
        }

        ko.bindingHandlers.htmlEditor = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                $(element).val(valueAccessor()).ckeditor();
            }
        }

        ko.bindingHandlers.threeStateButton = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                var defaults = { labels: ['None', 'False', 'True'], property: null };
                var va = valueAccessor();
                va = $.extend(defaults, va);
                var labels = va.labels
                $(element).append('<option value="">' + labels[0] + '</option>')
                    .append('<option value="false">' + labels[1] + '</option>')
                    .append('<option value="true">' + labels[2] + '</option>')
                    .bind('change', function (e) {
                        //console.log('change', e);
                        if (va.property)
                            bindingContext.$data[va.property] = $(element).val();
                    });
                if (va.property) {
                    var value = bindingContext.$data[va.property];
                    if (value === false)
                        $(element).find('option:nth-child(2)').prop('selected', true);
                    else if (value === true)
                        $(element).find('option:nth-child(3)').prop('selected', true);
                    else
                        $(element).find('option:nth-child(1)').prop('selected', true);
                }
            }
        }

        ko.bindingHandlers.fileupload = {
            init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
                var $fu = $('<div class="fileupload">'
                        + '<table class="table table-bordered"><tr><td colspan="3" class="btn-group">'
                        + '<span class="btn btn-primary fileinput-button"><b class="icon-upload"></b>&nbsp;' + tp.get('btn_upload_file') + '<input id="fileupload" type="file" name="files[]"></span>'
                        + '</td></tr></table>'
                        + '</div>').appendTo(element);
                //var $btn = $fu.find('.upload');
                var $tbl = $fu.find('table');

                var callback = null;
                var va = valueAccessor();
                $fu.fileupload({
                    url: va.url,
                    autoUpload: false,
                    dataType: 'json',
                    add: function (e, data) {
                        var file = data.files[0];
                        if (va.validate && va.validate(file) == false)
                            return;
                        $tbl.find('tr.info').detach().remove();
                        var tr = $(['<tr><td class="span1"><div class="icon-list-alt">&nbsp;</div></td><td class="span6">', file.name, '</td><td class="span5"><div class="progress"><div class="bar" style="width: 0%;"></div></div></td></tr>'].join('')).appendTo($tbl);
                        tr.addClass('info');
                        bindingContext.$root.submitFunc = function (cb) {
                            callback = cb;
                            data.submit();
                        }
                    },
                    progress: function (e, data) {
                        var progress = parseInt(data.loaded / data.total * 100, 10);
                        var bar = $tbl.find('.progress .bar');
                        bar.css({ width: progress + '%' });
                    },
                    send: function () {
                        var tr = $tbl.find('tr.info').last();
                        tr.removeClass('info').addClass('warning');
                    },
                    done: function () {
                        if (callback)
                            callback();
                    },
                    fail: function () {
                        msgr.error(tp.get('msg_file_upload_failed'));
                    }
                });
            }
        }
    });