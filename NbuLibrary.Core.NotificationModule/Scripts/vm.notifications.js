define('mods/vm.notifications',
    ['jquery', 'textprovider', 'presenter', 'messanger', 'grid', 'dataservice', 'usercontext', 'filters'],
    function ($, tp, presenter, msgr, entitygrid, ds, uc, gf) {

        var Modes = {
            Inbox: 'inbox',
            Sent: 'sent',
            Custom: 'custom'
        }

        var grid = new entitygrid.Grid();
        var filters = gf.GridFilters();
        var _mode = ko.observable();
        var _customOptions = ko.observable();
        function bind(mode, customOptions) {
            if ((mode === _mode() && mode != Modes.Custom)
                || (mode == Modes.Custom && customOptions && _customOptions() && customOptions.entity === _customOptions().entity && customOptions.role === _customOptions().role && customOptions.id === _customOptions().id)) {
                grid.reload();
                return;
            }

            _mode(mode);
            var query = ds.search('Notification');
            var uiDef = null;
            if (mode == Modes.Custom) {
                _customOptions(customOptions);
                query = ds.search('Notification');
                query.rules.relatedTo(customOptions.entity, customOptions.role, customOptions.id);
                query.rules.is('Archived', false);

                uiDef = "Notifications_All_Generic";
            }
            else if (mode == Modes.Inbox) {
                query.rules.relatedTo('User', 'Recipient', uc.currentUser().Id);
                uiDef = "Notifications_All_Inbox";

                filters.bind([
                    { Type: 5, Label: tp.get('filterby_received'), Property: 'Received', YesLabel: tp.get('filterby_received_yes'), NoLabel: tp.get('filterby_received_no') },
                    { Entity: 'User', Role: 'Sender', Type: 8, Property: 'Email', Label: tp.get('filterby_sender_email') },
                    { Type: 5, Label: tp.get('filterby_archived'), Property: 'Archived', YesLabel: tp.get('filterby_archived_yes'), NoLabel: tp.get('filterby_archived_no') },
                    { Type: 7, Label: tp.get('filterby_subject'), Property: 'Subject' },
                    { Type: 9, Label: tp.get('filterby_notification_date_after'), Property: 'Date', After: true },
                    { Type: 9, Label: tp.get('filterby_notification_date_before'), Property: 'Date', Before: true }
                ],
                    query, function () { grid.reload(true); }, function () { filters.Filters()[2].onChange({ added: { id: 1 } }); grid.reload(true); });
                filters.Filters()[2].onChange({ added: { id: 1 } });
            }
            else if (mode == Modes.Sent) {
                query.rules.relatedTo('User', 'Sender', uc.currentUser().Id);
                uiDef = "Notifications_All_Sent";
                filters.bind([
                        { Entity: 'User', Role: 'Recipient', Type: 8, Property: 'Email', Label: tp.get('filterby_sender_email') },
                        { Type: 5, Label: tp.get('filterby_archived'), Property: 'ArchivedSent', YesLabel: tp.get('filterby_archived_yes'), NoLabel: tp.get('filterby_archived_no') },
                        { Type: 7, Label: tp.get('filterby_subject'), Property: 'Subject' },
                        { Type: 9, Label: tp.get('filterby_notification_date_after'), Property: 'Date', After: true },
                        { Type: 9, Label: tp.get('filterby_notification_date_before'), Property: 'Date', Before: true }
                ],
                    query, function () { grid.reload(true); }, function () { filters.Filters()[1].onChange({ added: { id: 1 } }); grid.reload(true); });
                filters.Filters()[1].onChange({ added: { id: 1 } });
            }
            else
                throw Error("Unknown binding mode for vm.notifications");

            query.addProperty("Received");
            query.addProperty('Archived');
            query.addProperty('ArchivedSent');
            query.sort('Date', true);

            grid.bind(query, uiDef);
        }

        return {
            bind: bind,
            Mode: _mode,
            template: 'Notifications_Grid',
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters: filters,
            sort: grid.sort,
            Mode: _mode,
            archive: function (notif) {
                var update = ds.update('Notification', notif.Id);
                update.set(_mode() == 'inbox' ? 'Archived' : 'ArchivedSent', true);
                $.when(update.execute()).done(function (r) {
                    if (r.Success) {
                        msgr.success(tp.get('msg_notification_archived_ok'));
                        grid.reload();
                    }
                });
            }
        };
    });