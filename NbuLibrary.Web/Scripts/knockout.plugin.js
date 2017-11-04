ko.showEntity = function (item, template) {
    var rs = template.indexOf('{');
    var ls = template.indexOf('}');
    var prevrs = -1;
    var prevls = -1;
    var res = [];
    while(rs >= 0)
    {
        res.push(template.substring(prevls+1, rs));
        res.push(item[template.substring(rs + 1, ls)]);
        prevls = ls;
        rs = template.indexOf('{', ls);
        ls = template.indexOf('}', ls+1);
    }

    res.push(template.substring(prevls+1, template.length));

    return res.join('');
}

ko.bindingHandlers.viewText = {
    init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
    },
    update: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
        // This will be called once when the binding is first applied to an element,
        // and again whenever the associated observable changes value.
        // Update the DOM element based on the supplied values here.
        var field = valueAccessor().Field;
        var item = valueAccessor().Item;
        //console.log(valueAccessor());

        if (field.Entity) {
            var relItem = item[field.Entity + "_" + field.Role];
            if (typeof (relItem) == "function")
                relItem = relItem();
            if (relItem && relItem.length) {
                if (relItem.length > 1) {
                    var res = [];
                    $(relItem).each(function () {
                        res.push(this[field.Property]);
                    });
                    $(element).text(res.join('; '));
                }
                else
                    $(element).text(relItem[0][field.Property]);
            }
        }
        else if (field.Type == 3) { // enumlist
            var es = new NbuLib.Core.EntityStore();
            var en = es.getEnum(field.EnumClass);
            var val = item[field.Property]();
            if (val !== null && val !== undefined)
                $(element).text(en[val]);
        }
        else if (field.Type === 4) {
            if (!!item[field.Property]()) {
                $('<i class="icon-ok"> </i>').appendTo(element);
                $(element).find('.icon-remove').remove();
            }

            else {
                $('<i class="icon-remove"> </i>').appendTo(element);
                $(element).find('.icon-ok').remove();
            }
        }
        else if (field.Type === 0 && field.Multiline) {
            var text = item[field.Property]();
            if (text && field.Length) {

                var str = text.substring(0, field.Length);
                if (str.length < text.length)
                    str += '...';
                text = str;
            }
            $('<pre>' + text + '</pre>').appendTo(element);
        }
        else if (field.Type === 0) { //textfield
            var text = item[field.Property]();
            if (text && field.Length) {

                var str = text.substring(0, field.Length);
                if (str.length < text.length)
                    str += '...';
                text = str;
            }
            $(element).text(text);
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
            $(element).text(item[field.Property]());
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

        //if (field.Entity) {
        //var edit = field.Edit;
        //var select = $('<select />').attr({ id: field.Property, name: field.Property });
        //if (!field.Edit.Single)
        //    select.prop('multiple', true);
        //client.search({ Entity: field.Entity }, function (data) {
        //    select.append($('<option></option>'));
        //    $(data).each(function () {
        //        var optEl = $('<option value="' + this.Id + '">' + this[field.Property] + '</option>').appendTo(select);
        //        var currentItem = item[field.Entity + "_" + field.Role];
        //        if (!currentItem)
        //            return;
        //        else if (currentItem.length == 0)
        //            return;

        //        if (currentItem.length == 1) {
        //            if (currentItem[0].Id == this.Id)
        //                optEl.prop('selected', true);
        //        }
        //        else if (currentItem.length > 1) {
        //            for (var x in currentItem) {
        //                if (this.Id == currentItem[x].Id) {
        //                    optEl.prop('selected', true);
        //                    break;
        //                }
        //            }
        //        }
        //    });
        //});
        //select.appendTo(element).bind('change', function (e) {
        //    var selected = select.children(':selected');
        //    var entityItem = { Id: selected.val(), EntityName: field.Entity };
        //    if (!entityItem[field.Property])
        //        entityItem[field.Property] = [];
        //    if (field.Edit.Single) {
        //        entityItem[field.Property].push(selected.text());
        //    }
        //    item[field.Entity + "_" + field.Role] = entityItem;
        //});

        //if (field.Edit.Required)
        //    $(select).rules('add', {
        //        notnull: true
        //    });
        //}
        //else 
        if (field.Type === 0 && !field.Multiline) // textbox
        {
            var input = $('<input type="text" placeholder="' + field.Label + '" ' + (item[field.Property]() ? 'value="' + item[field.Property]() + '"' : '') + ' />')
                .attr({ id: field.Property, name: field.Property })
                .appendTo(element);

            input.bind('keyup', function () {
                item[field.Property](input.val());
            });
            if (field.Required)
                $(input).rules('add', {
                    required: true
                });
        }
        else if (field.Type === 0 && field.Multiline) // textbox
        {
            var textarea = $('<textarea placeholder="' + field.Label + '">' + item[field.Property]() + ' </textarea>')
                .attr({ id: field.Property, name: field.Property })
                .appendTo(element)
                .width(500)
                .height(150);

            textarea.bind('keyup', function () {
                item[field.Property](textarea.val());
            });
            if (field.Required)
                $(textarea).rules('add', {
                    required: true
                });

            if(field.MinLength)
                $(textarea).rules('add', {
                    minlength: field.MinLength
                });

            if (field.MaxLength)
                $(textarea).rules('add', {
                    maxlength: field.MaxLength
                });
        }

            //else if (field.Type == 1) {
            //    var textarea = $('<textarea type="text" placeholder="' + field.Label + '">' + (item[field.Property]() ? item[field.Property]() : '') + '</textarea>')
            //        .attr({ id: field.Property, name: field.Property })
            //        .appendTo(element);

            //    if (field.Edit.Required)
            //        $(textarea).rules('add', {
            //            required: true
            //        });

            //    textarea.width(500);
            //    var maxRows = field.Edit.MaxLength / 50;

            //    textarea[0].rows = maxRows > 10 ? 10 : maxRows;

            //    textarea.bind('keyup', function () {
            //        item[field.Property](textarea.val());
            //    });
            //}
        else if (field.Type == 1) { //Number
            var input = $('<input type="text" placeholder="' + field.Label + '" />')
               .attr({ id: field.Property, name: field.Property })
               .appendTo(element);

            if (!!item[field.Property]()) {
                if (field.Integer)
                    input.val(parseInt(item[field.Property]()));
                else
                    input.val(parseFloat(item[field.Property]()));
            }
            input.bind('keyup', function () {
                item[field.Property](input.val());
            });

            $(input).rules('add', {
                required: field.Required,
                number: true,
                integer: field.Integer
            });
        }
        else if (field.Type == 2) { //Date
            var input = $('<input type="text" placeholder="' + field.Label + '" />').attr({ id: field.Property, name: field.Property });
            input.appendTo(element)
                .datepicker()
                .bind('changeDate', function (e) {
                    item[field.Property](e.date.toISOString());
                });
            var date = NbuLib.Convert.stringToDate(item[field.Property]());
            input.datepicker('setDate', date);
            if (field.Required)
                $(input).rules('add', {
                    required: true
                });
        }
        else if (field.Type == 3) { // enumlist
            var select = $('<select />').attr({ id: field.Property, name: field.Property });
            if(!field.Multiple) select.append($('<option></option>'));
            var es = new NbuLib.Core.EntityStore();
            var en = es.getEnum(field.EnumClass);
            $.each(en, function (k, v) {
                var opt = $('<option value="' + k + '">' + v + '</option>').appendTo(select);
                if (item[field.Property]() == k)
                    opt.attr('selected', true);
            });

            select.appendTo(element).bind('change', function (e) {
                item[field.Property](select.val());
            });

            select.select2({ width: 'resolve', placeholder: "Select", allowClear: true });

            if (field.Required)
                $(select).rules('add', {
                    notnull: true
                });
        }
        else if (field.Type === 4) {
            var vals = [];
            var select = $('<select />').attr({ id: field.Property, name: field.Property });
            if (!field.Multiple) select.append($('<option></option>'));
            if (field.Multiple)
                select.prop('multiple', true);
            client.search({ Entity: field.Entity }, function (data) {
                $(data).each(function () {
                    var txt = ko.showEntity(this, field.Formula);
                    var optEl = $('<option value="' + this.Id + '">' + txt + '</option>').appendTo(select);
                    var currentItem = item[field.Entity + "_" + field.Role];
                    if (!currentItem)
                        return;
                    else if (currentItem.length == 0)
                        return;

                    if (currentItem.length == 1) {
                        if (currentItem[0].Id == this.Id)
                            optEl.prop('selected', true);
                        vals.push(this.Id);
                    }
                    else if (currentItem.length > 1) {
                        for (var x in currentItem) {
                            if (this.Id == currentItem[x].Id) {
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
                    //console.log('selectchange', e);

                    if (!item[field.Property])
                        item[field.Property] = [];


                    var relItem = {
                        Entity: field.Entity,
                        Role: field.Role
                    };

                    if (e.added) {
                        bindingContext.$root.addRelation(relItem, { Id: e.added.id });
                    }
                    else if (e.removed) {
                        bindingContext.$root.removeRelation(relItem, { Id: e.removed.id });
                    }
                    else {
                        bindingContext.$root.addRelation(relItem, { Id: parseInt(select.val()) });
                    }

                    //item[field.Entity + "_" + field.Role] = entityItem;
                });

                if (field.Required)
                    $(select).rules('add', {
                        notnull: true
                    });
                //select.val(vals);
            });
        }
        else if (field.Type === 5) { //checkbox
            $('<input type="checkbox">')
                .prop('checked', !!item[field.Property]())
                .appendTo(element).change(function (e) {
                    //console.log('checkbox', $(this).val());
                    item[field.Property]($(this).prop('checked'));
                });
        }
        else {
            var input = $('<input type="text" placeholder="' + field.Label + '" ' + (item[field.Property]() ? 'value="' + item[field.Property]() + '"' : '') + ' />')
                .attr({ id: field.Property, name: field.Property })
                .appendTo(element);

            input.bind('keyup', function () {
                item[field.Property](input.val());
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
        if (field.Type == 0 && !field.Entity) {
            $(self.element).find('input').val(item[field.Property]());
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
    }
}



//Author: Ryan Niemeyer
//http://www.knockmeout.net/2011/03/guard-your-model-accept-or-cancel-edits.html
//==========================================================================================

//wrapper to an observable that requires accept/cancel
ko.protectedObservable = function (initialValue) {
    //private variables
    var _actualValue = ko.observable(initialValue),
        _tempValue = initialValue;

    //computed observable that we will return
    var result = ko.computed({
        //always return the actual value
        read: function () {
            return _actualValue();
        },
        //stored in a temporary spot until commit
        write: function (newValue) {
            _tempValue = newValue;
        }
    });

    //if different, commit temp value
    result.commit = function () {
        if (_tempValue !== _actualValue()) {
            _actualValue(_tempValue);
        }
    };

    result.hasChanges = function () {
        return _tempValue !== _actualValue();
    };

    //force subscribers to take original
    result.reset = function () {
        _actualValue.valueHasMutated();
        _tempValue = _actualValue();   //reset temp value
    };

    return result;
};
//==========================================================================================