/// <reference path="../knockout-2.2.1.debug.js" />

define('vm.profileupdate',
    ['jquery', 'ko', 'dataservice', 'viewservice', 'entitymapper', 'messanger', 'presenter', 'textprovider', 'datacontext', 'entitypage', 'usercontext', 'datacontext'],
    function ($, ko, dataservice, viewservice, mapper, msgr, presenter, tp, dc, EntityPage, usercontext, dc) {
        var page = new EntityPage.Page();

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

        var _entity = {};
        var _needsReActivation = ko.observable(false);
        var _group = ko.observable();
        var _query = null;
        var _groups = ko.observableArray();
        var _loaded = ko.observable(false);
        function bind(id) {
            _loaded(false);
            _query = dataservice.get('User', id);
            $.when(loadData(id)).done(function () {
                function needsReactivationUpdate(newVal) {
                    _needsReActivation(true);
                };
                _entity.Data.FacultyNumber.subscribe(needsReactivationUpdate);
                _entity.Data.CardNumber.subscribe(needsReactivationUpdate);
                _loaded(true);
            });
        };

        (function () {
            var q = dataservice.search('UserGroup');
            q.allProperties(true);
            q.rules.is('UserType', usercontext.currentUser().UserType);
            $.when(q.execute()).done(function (groups) {
                ko.utils.arrayForEach(groups, function (uc) {
                    _groups.push({ label: uc.Data.Name, id: uc.Id });
                });
            });
        })();

        var fields = [
            { Label: tp.get('lbl_profile_email'), Property: 'Email', Type: EditTypes.Textbox, Required: true, RegEx: /^(([^<>()[\]\\.,;:\s@\"]+(\.[^<>()[\]\\.,;:\s@\"]+)*)|(\".+\"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/ }, // 0
			{ Label: tp.get('lbl_profile_fname'), Property: 'FirstName', Type: EditTypes.Textbox, Required: true }, // 1
			{ Label: tp.get('lbl_profile_mname'), Property: 'MiddleName', Type: EditTypes.Textbox, Required: false }, // 2
			{ Label: tp.get('lbl_profile_lname'), Property: 'LastName', Type: EditTypes.Textbox, Required: true }, // 3
			{ Label: tp.get('lbl_profile_phone'), Property: 'PhoneNumber', Type: EditTypes.Textbox, Required: false }, // 4
			{ Label: tp.get('lbl_profile_cardnumber'), Property: 'CardNumber', Type: EditTypes.Textbox, Required: false }, // 5,
			{ Label: tp.get('lbl_profile_facultynumber'), Property: 'FacultyNumber', Type: EditTypes.Textbox, Required: true, RegEx: '[f,p,F,P][0-9]+' } // 6

        ];
        var displayedField = ko.computed(function () {
            var r = [];
            if (_group() == 'Външен (с читателска карта)') {
                fields[5].Required = true;
                return [fields[0], fields[1], fields[2], fields[3], fields[4], fields[5]];
            }
            else if (_group() == 'Студент' || _group() == 'Преподавател') {
                fields[5].Required = false;
                return [fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6]];
            }
            else
            {
                return [fields[0], fields[1], fields[2], fields[3], fields[4]];
            }
        });

        function loadData(id) {
            return $.Deferred(function (deferred) {
                ko.utils.arrayForEach(fields, function (fld) {
                    if (fld.Entity && fld.Role)
                        _query.include(fld.Entity, fld.Role);
                    else
                        _query.addProperty(fld.Property);
                });
                _query.include('UserGroup', 'UserGroup');

                $.when(_query.execute())
				.done(function (data) {
				    if (data) {
				        $.when(mapper.map('User', data, _entity))
						.done(function () {
						    _loaded(true);
						    dc.bind('User', _entity, id);
						    _group(_entity.RelationsData.UserGroup_UserGroup().Entity.Data.Name);
						    deferred.resolve(_entity);
						});
				    }
				});
            }).promise();
        };

        function onGroupChange(e) {
            if (e.added) {
                dc.addRelation('UserGroup', 'UserGroup', e.added.id);
                _loaded(false);
                _needsReActivation(true);
                _group(e.added.text);
                _loaded(true);
            }
        }

        return {
            template: 'Profile_UpdateInfo',
            bind: bind,
            Loaded: _loaded,
            Entity: _entity,
            Fields: displayedField,
            save: function () {
                if ($('#viewport').find('form').valid()) {
                    var ask = null;
                    function saveChanges() {
                        if(ask) ask.close();
                        $.when(dc.save()).done(function (r) {
                            if (r.Success) {
                                msgr.success(tp.get('msg_saved_ok'));
                                if (r.Data['account_warning_logged_out']) {
                                    msgr.warning(tp.get('msg_you_will_be_logged_out'));
                                    setTimeout(function () {
                                        window.location = '/';
                                    }, 2500);
                                }
                                if (r.Data['account_event_email_changed'])
                                {
                                    msgr.warning(tp.get('msg_your_email_was_changed_to_template').replace('{Email}', r.Data['account_event_email_changed']));
                                    setTimeout(function () {
                                        window.location = '/';
                                    }, 3000);
                                }
                            }
                        });
                    }
                    if (_needsReActivation()) {
                        ask = presenter.ask(tp.get('confirm_profile_update_with_reactivation'));
                        ask.yes(saveChanges);
                        ask.show();
                    }
                    else
                        saveChanges();
                }
            },
            IsDirty: dc.hasChanges,
            Groups: _groups,
            NeedsReactivation: _needsReActivation,
            userGroupChanged: onGroupChange
        };
    });