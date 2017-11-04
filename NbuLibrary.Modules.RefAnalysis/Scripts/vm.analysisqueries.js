define('mods/vm.analysisqueries',
    ['dataservice', 'usercontext', 'datacontext', 'textprovider', 'presenter', 'messanger', 'grid', 'entitypage', 'filters'],
    function (ds, usercontext, dc, tp, presenter, msgr, entitygrid, ep, gf) {
        var EntityConsts = {
            AnalysisQuery: 'AnalysisQuery'
        };

        var QueryStatuses = {
            New: 0,
            InProcess: 1,
            Completed: 2,
            Canceled: 3,
            Rejected: 4
        };
        var grid = new entitygrid.Grid();
        var filters = new gf.GridFilters();

        var _bind = false;
        function bind() {
            if (_bind)
                grid.reload();
            else {
                var query = ds.search(EntityConsts.AnalysisQuery);
                query.addProperty('Status');
                query.addProperty('PaymentMethod');
                query.include('Payment', 'Payment');
                if (usercontext.isCustomer()) {
                    query.rules.relatedTo('User', 'Customer', usercontext.currentUser().Id);
                }
                query.sort('CreatedOn', true);
                filters.bind([
                    { Multiple: true, Type: 3, Label: tp.get('filterby_query_status'), Property: 'Status', EnumClass: 'NbuLibrary.Core.Domain.QueryStatus, NbuLibrary.Core.Domain, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' }
                ],
                    query, function () { grid.reload(true); }, function () { grid.reload(true); });
                filters.Filters()[0].value(usercontext.isCustomer() ? [0, 1, 2] : [0, 1]);
                grid.bind(query, usercontext.isCustomer() ? 'RefAnalysis_Customer_AnalysisQuery_Grid' : 'RefAnalysis_Librarian_AnalysisQuery_Grid');
                _bind = true;
            }
        }

        function canEdit(query) {
            if (!usercontext.isCustomer())
                return false;

            return query.Data.Status() == QueryStatuses.New;
        }

        function cancel(query) {
            var ask = presenter.ask(tp.get('ask_customer_cancel_query'));
            ask.yes(function () {
                var update = ds.update(EntityConsts.AnalysisQuery, query.Id());
                update.set('Status', QueryStatuses.Canceled);
                $.when(update.execute()).done(function () {
                    msgr.success(tp.get('msg_query_canceled'));
                    grid.reload();
                });
            });
            ask.show();
        }

        function sendNotification(id, queryUpdate) {
            var page = new ep.Page();
            page.bind('Notification', 'Notifications_All_SendNew');
            page.template = 'GeneralForm';

            var getData = ds.get(EntityConsts.AnalysisQuery, id);
            getData.addProperty('ReplyMethod');
            getData.addProperty('Number');
            getData.include('User', 'Customer');
            var loadActualData = getData.execute();
            $.when(page.loadDefaults()).done(function () {
                $.when(loadActualData).done(function (d) {
                    dc.bind('Notification', page.Entity);
                    page.Entity = dc.getEntity();
                    dc.set('Subject', tp.get('query_rejected_subject').replace('{query::status}', d.Data.Number));
                    dc.set('Method', d.Data.ReplyMethod);
                    dc.addRelation('User', 'Sender', usercontext.currentUser().Id);
                    dc.addRelation('User', 'Recipient', d.RelationsData.User_Customer.Entity.Id);
                    dc.addRelation(EntityConsts.AnalysisQuery, 'Notification', d.Id);
                    var popup = presenter.popup(page, { title: tp.get('entity_notification') });
                    popup.ok(tp.get('btn_send'), function () {
                        if (dc.hasChanges()) {
                            $.when(dc.save(), queryUpdate.execute())
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

        function process(query) {
            function processInternal() {
                var page = new ep.Page();
                page.bind(EntityConsts.AnalysisQuery, 'RefAnalysis_AnalysisQuery_Process', query.Id());
                page.template = 'GeneralForm';
                $.when(page.load()).done(function () {
                    dc.bind(EntityConsts.AnalysisQuery, page.Entity, query.Id());
                    var popup = presenter.popup(page, { title: tp.get('heading_process_query') });
                    popup.ok(tp.get('btn_process'), function () {
                        if (dc.hasChanges()) {
                            var status = dc.getEntity().Data.Status();
                            if (status == QueryStatuses.Rejected) {
                                var update = ds.update(EntityConsts.AnalysisQuery, query.Id());
                                dc.applyChanges(update);
                                dc.clear();
                                sendNotification(query.Id(), update);
                                popup.close();
                            }
                            else {
                                function saveProcess() {
                                    $.when(dc.save())
                                    .done(function (res) {
                                        dc.clear();
                                        popup.close();
                                        msgr.success(tp.get('msg_saved_ok'));
                                        grid.reload();
                                        if (page.Entity.Data.Status() == QueryStatuses.Completed)
                                            editPayment(query);
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
                        }
                    });
                    popup.show();
                });
            };

            //TODO: processed by shoud be onetomany rel
            if (query.RelationsData.User_ProcessedBy && query.RelationsData.User_ProcessedBy() && query.RelationsData.User_ProcessedBy().Entity.Id != usercontext.currentUser().Id) {
                var ask = presenter.ask(tp.get('ask_query_processed_by_other'));
                ask.yes(processInternal);
                ask.show();
            }
            else
                processInternal();
        }

        function editPayment(query) {
            var page = new ep.Page();
            var exPayment = null;
            if (query.RelationsData && query.RelationsData.Payment_Payment)
                exPayment = query.RelationsData.Payment_Payment();

            var pid = exPayment ? exPayment.Entity.Id : null;
            page.bind('Payment', 'Finance_Librarian_PaymentForm', pid);
            page.template = 'GeneralForm';

            var job = exPayment != null ? page.load() : page.loadDefaults();
            $.when(job).done(function () {
                dc.bind('Payment', page.Entity, pid);
                if (exPayment == null) {
                    page.Entity = dc.getEntity();
                    dc.set('Method', query.Data.PaymentMethod());
                    dc.set('Status', 1);
                }
                var popup = presenter.popup(page, { title: tp.get('entity_payment') });
                popup.ok(tp.get('btn_create_payment'), function () {
                    dc.addRelation(EntityConsts.AnalysisQuery, 'Payment', query.Id());
                    $.when(dc.save()).done(function () {
                        popup.close();
                        grid.reload();
                        msgr.success(tp.get('msg_saved_ok'));
                    });
                });
                popup.show();
            });
        }

        return {
            bind: bind,
            template: 'RefAnalysis_AnalysisQuery_Grid',
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters: filters,
            IsLibrarian: usercontext.isLibrarian(),
            sort: grid.sort,
            canEdit: canEdit,
            canCancel: canEdit,
            editPayment: editPayment,
            cancel: cancel,
            process: process
        };
    });