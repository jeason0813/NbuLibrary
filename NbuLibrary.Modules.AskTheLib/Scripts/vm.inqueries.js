define('mods/vm.inqueries',
    ['jquery', 'ko', 'textprovider', 'dataservice', 'datacontext', 'messanger', 'grid', 'usercontext', 'entitypage', 'presenter', 'filters', 'mods/vm.inquery'],
    function ($, ko, tp, ds, dc, msgr, entitygrid, usercontext, ep, presenter, gf, vmInquery) {

        var INQUERY = 'Inquery';
        var QueryStatuses = {
            New: 0,
            InProcess: 1,
            Completed: 2,
            Canceled: 3,
            Rejected: 4
        };

        var query = null;
        var template = usercontext.isCustomer() ? "AskTheLib_Customer_Inqueries" : "AskTheLib_Librarian_Inqueries";
        var grid = new entitygrid.Grid();
        var filters = new gf.GridFilters();

        var _bind = false;

        function bind() {
            if (!_bind) {
                query = ds.search(INQUERY);
                query.addProperty("Status");
                query.sort('CreatedOn', true);

                filters.bind([
                    { Multiple: true, Type: 3, Label: tp.get('filterby_query_status'), Property: 'Status', EnumClass: 'NbuLibrary.Core.Domain.QueryStatus, NbuLibrary.Core.Domain, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' }
                ],
                    query, function () { grid.reload(true); }, function () { grid.reload(true); });
                filters.Filters()[0].value([0, 1, 2]);
                var uiDef = 'AskTheLib_Librarian_InqueryGrid';
                if (usercontext.isCustomer()) {
                    query.rules.relatedTo('User', 'Customer', usercontext.currentUser().Id);
                    uiDef = 'AskTheLib_Customer_InqueryGrid';
                }

                if (usercontext.isLibrarian()) {
                    query.include('User', 'ProcessedBy');
                }

                grid.bind(query, uiDef);
                _bind = true;
            }
            else
                grid.reload();
        };

        function canEdit(inquery) {
            if (!usercontext.isCustomer())
                return false;

            return inquery.Data.Status() == QueryStatuses.New;
        }

        function process(inquery) {

            function processInternal() {
                var page = new ep.Page();
                page.bind(INQUERY, 'AskTheLib_Librarian_InqueryProcess', inquery.Id());
                page.template = 'GeneralForm';
                $.when(page.load()).done(function () {
                    dc.bind(INQUERY, page.Entity, inquery.Id());
                });
                var popup = presenter.popup(page, { title: tp.get('heading_process_query') });
                popup.ok(tp.get('btn_process'), function () {
                    if (dc.hasChanges()) {
                        var status = dc.getEntity().Data.Status();
                        if (status == QueryStatuses.Completed || status == QueryStatuses.Rejected) {
                            var update = ds.update(INQUERY, inquery.Id());
                            dc.applyChanges(update);
                            dc.clear();
                            sendNotification(inquery.Id(), status, update);
                            popup.close();
                            return;
                        }

                        function saveProcess() {
                            $.when(dc.save()).done(function () {
                                popup.close();
                                msgr.success(tp.get('msg_saved_ok'));
                                dc.clear();
                                grid.reload();
                            });
                        }

                        if (status == QueryStatuses.Canceled && usercontext.isLibrarian()) {
                            var ask = presenter.ask(tp.get('ask_query_about_to_cancel'));
                            ask.yes(saveProcess);
                            ask.show();
                        }
                        else
                            saveProcess();
                    }
                });
                popup.show();
            };

            //TODO: processed by shoud be onetomany rel
            if (inquery.RelationsData.User_ProcessedBy && inquery.RelationsData.User_ProcessedBy() && inquery.RelationsData.User_ProcessedBy().Entity.Id != usercontext.currentUser().Id) {
                var ask = presenter.ask(tp.get('ask_query_processed_by_other'));
                ask.yes(processInternal);
                ask.show();
            }
            else
                processInternal();
        }

        function sendNotification(id, status, inqueryUpdate) {
            var page = new ep.Page();
            page.bind('Notification', 'Notifications_All_SendNew');
            page.template = 'GeneralForm';

            var getData = ds.get(INQUERY, id);
            getData.addProperty('ReplyMethod');
            getData.addProperty('Number');
            getData.include('User', 'Customer');
            var loadActualData = getData.execute();
            $.when(page.loadDefaults()).done(function () {
                $.when(loadActualData).done(function (d) {
                    dc.bind('Notification', page.Entity);
                    page.Entity = dc.getEntity();
                    dc.set('Subject', status == QueryStatuses.Completed ? tp.get('askthelib_inquery_compelte_subject') : tp.get('query_rejected_subject').replace('{query::status}', d.Data.Number));
                    dc.set('Method', d.Data.ReplyMethod);
                    dc.addRelation('User', 'Sender', usercontext.currentUser().Id);
                    dc.addRelation('User', 'Recipient', d.RelationsData.User_Customer.Entity.Id);
                    dc.addRelation(INQUERY, INQUERY, d.Id);
                    var popup = presenter.popup(page, { title: tp.get('entity_notification') });
                    popup.ok(tp.get('btn_send'), function () {
                        if (dc.hasChanges()) {
                            $.when(dc.save(), inqueryUpdate.execute())
                            .done(function (res) {
                                dc.clear();
                                popup.close();
                                msgr.success(tp.get('msg_saved_ok'));
                                grid.reload();
                            });
                        }
                    });
                    popup.show();
                });

            });
        }

        function cancel(inquery) {
            var ask = presenter.ask(tp.get('ask_customer_cancel_query'));
            ask.yes(function () {
                var update = ds.update(INQUERY, inquery.Id());
                update.set('Status', QueryStatuses.Canceled);
                $.when(update.execute()).done(function () {
                    msgr.success(tp.get('msg_query_canceled'));
                    grid.reload();
                });
            });
            ask.show();
        }

        function create() {
            vmInquery.bind(null, true);
            var popup = presenter.popup(vmInquery, { title: tp.get('entity_inquery') });
            popup.ok(tp.get('btn_create_query'), function () {
                $.when(vmInquery.save()).done(function () {
                    popup.close();
                });
            });
            popup.show();
        }

        return {
            bind: bind,
            template: template,
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters: filters,
            sort: grid.sort,
            canEdit: canEdit,
            canCancel: canEdit,
            process: process,
            cancel: cancel,
            create: create
        };
    });


