define('mods/vm.biblqueries',
    ['dataservice', 'usercontext', 'datacontext', 'textprovider', 'presenter', 'messanger', 'grid', 'entitypage', 'filters', 'mods/vm.bibliography'],
    function (ds, usercontext, dc, tp, presenter, msgr, entitygrid, ep, gf, vmBibl) {

        var EntityConsts = {
            Bibliography: 'Bibliography',
            BibliographicDocument: 'BibliographicDocument',
            Language: 'Language',
            BibliographicQuery: 'BibliographicQuery'
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
                var query = ds.search(EntityConsts.BibliographicQuery);
                query.addProperty('Status');
                query.addProperty('ForNew');
                query.addProperty('PaymentMethod');
                query.include('Payment', 'Payment');
                query.include('Bibliography', 'Query');
                if (usercontext.isCustomer()) {
                    query.rules.relatedTo('User', 'Customer', usercontext.currentUser().Id);
                }
                query.sort('CreatedOn', true);
                filters.bind([
                    { Multiple: true, Type: 3, Label: tp.get('filterby_query_status'), Property: 'Status', EnumClass: 'NbuLibrary.Core.Domain.QueryStatus, NbuLibrary.Core.Domain, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' }
                ],
                    query, function () { grid.reload(true); }, function () { grid.reload(true); });
                filters.Filters()[0].value(usercontext.isCustomer() ? [0, 1, 2] : [0, 1]);
                grid.bind(query, usercontext.isCustomer() ? 'BiblRef_Customer_BibliographicQuery_Grid' : 'BiblRef_Librarian_BibliographicQuery_Grid');
                _bind = true;
            }
        }

        function canEdit(query) {
            return query.Data.Status() == QueryStatuses.New;
        }

        function cancel(query) {
            var ask = presenter.ask(tp.get('ask_customer_cancel_query'));
            ask.yes(function () {
                var update = ds.update(EntityConsts.BibliographicQuery, query.Id());
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

            var getData = ds.get(EntityConsts.BibliographicQuery, id);
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
                    dc.addRelation(EntityConsts.BibliographicQuery, 'Notification', d.Id);
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

        function editQuery(query) {
            var page = new ep.Page();
            page.bind(EntityConsts.BibliographicQuery, 'BiblRef_Customer_BibliographicQuery_Form', query.Id());
            page.template = 'GeneralForm';
            $.when(page.load()).done(function () {
                dc.bind(EntityConsts.BibliographicQuery, page.Entity, query.Id());
                var popup = presenter.popup(page, { title: tp.get('heading_edit_query') });
                popup.ok(tp.get('btn_save'), function () {
                    if (dc.hasChanges()) {
                        $.when(dc.save())
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
        }

        function editBibliography(query) {
            var q = ds.search(EntityConsts.Bibliography);
            q.rules.relatedTo(EntityConsts.BibliographicQuery, 'Query', query.Id());
            $.when(q.execute()).done(function (results) {
                if (!results || results.length == 0)
                    return msgr.error(tp.get('msg_bibliography_not_found'));

                var bibl = results[0];
                vmBibl.bind(bibl.Id);
                var popup = presenter.popup(vmBibl, { title: tp.get('heading_biblref_bibliographycreate') });
                popup.ok(tp.get('btn_save'), function () {
                    if (!popup.getForm().valid())
                        return;

                    var validKeywords = true;
                    ko.utils.arrayForEach(vmBibl.Keywords(), function (kw) {
                        if (!kw.Keywords() || kw.Keywords().length == 0)
                            validKeywords = false;
                    });
                    if (!validKeywords)
                        return msgr.error(tp.get('msg_should_fill_keywords'));

                    $.when(vmBibl.getUpdate().execute()).done(function () {
                        msgr.success(tp.get('msg_saved_ok'))
                        popup.close();
                        grid.reload();
                    });

                });
                popup.show();
            });
        }

        function process(query) {

            function processInternal() {
                var page = new ep.Page();
                page.bind(EntityConsts.BibliographicQuery, 'BiblRef_BibliographicQuery_Process', query.Id());
                page.template = 'GeneralForm';
                $.when(page.load()).done(function () {
                    dc.bind(EntityConsts.BibliographicQuery, page.Entity, query.Id());
                    var popup = presenter.popup(page, { title: tp.get('heading_process_query') });
                    popup.ok(tp.get('btn_process'), function () {
                        if (dc.hasChanges()) {
                            var status = dc.getEntity().Data.Status();
                            if (status == QueryStatuses.Rejected) {
                                var update = ds.update(EntityConsts.BibliographicQuery, query.Id);
                                dc.applyChanges(update);
                                dc.clear();
                                sendNotification(query.Id(), update);
                                popup.close();
                            }
                            else if (page.Entity.Data.Status() == QueryStatuses.Completed) {
                                var update = ds.update(EntityConsts.BibliographicQuery, query.Id);
                                dc.applyChanges(update);
                                completeBibliography(query, update);
                                popup.close();
                            } else {
                                function saveProcess() {
                                    $.when(dc.save())
                                    .done(function (res) {
                                        dc.clear();
                                        popup.close();
                                        msgr.success(tp.get('msg_saved_ok'));
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

        function completeBibliography(query, updateQuery) {
            var page = new ep.Page();
            page.bind(EntityConsts.Bibliography, 'BiblRef_Bibliography_CompletionForm', query.RelationsData.Bibliography_Query().Entity.Id);
            page.template = 'GeneralForm';
            $.when(page.load()).done(function () { dc.bind(EntityConsts.Bibliography, page.Entity, page.Entity.Id()); });
            var popup = presenter.popup(page, { title: tp.get('heading_biblref_bibliographycompletion') });
            popup.ok(tp.get('btn_complete_bibliography'), function () {
                dc.set('Complete', true);
                $.when(dc.save()).done(function (res1) {
                    $.when(updateQuery.execute()).done(function () {
                        msgr.success(tp.get('msg_saved_successfully'));
                        editPayment(query);
                        popup.close();
                    });
                });
            });
            popup.show();
        }

        function editPayment(query, updateQuery, updateBibl) {
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
                    function savePayment() {
                        dc.addRelation(EntityConsts.BibliographicQuery, 'Payment', query.Id());
                        $.when(dc.save()).done(function () {
                            popup.close();
                            grid.reload();
                            msgr.success(tp.get('msg_saved_successfully'));
                        });
                    };
                    if (updateQuery && updateBibl) {
                        throw new Error("Not implemented yet.");
                    }
                    else
                        savePayment();
                });
                popup.show();
            });
        }

        return {
            bind: bind,
            template: 'BiblRef_BibliographicQuery_Grid',
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
            process: process,
            editQuery: editQuery,
            editBibliography: editBibliography
        };
    });


