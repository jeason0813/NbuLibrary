define('mods/vm.payments',
    ['jquery', 'router', 'usercontext', 'entityservice', 'dataservice', 'textprovider', 'presenter', 'grid', 'filters'],
    function ($, router, usercontext, es, dataservice, tp, presenter, entitygrid, gf) {
        var grid = new entitygrid.Grid();
        var filters = new gf.GridFilters();
        var _relations = [];
        var _waitingOnly = undefined;
        function bind(waiting) {
            if (_waitingOnly === waiting && _waitingOnly !== undefined)
                grid.reload();
            else {
                var query = dataservice.search('Payment');
                query.sort('CreatedOn', true);
                query.rules.relatedTo('User', 'Customer', usercontext.currentUser().Id);
                query.addProperty('Status');
                var loadRelsJob = es.getEntity('Payment');
                $.when(loadRelsJob).done(function (e) {
                    ko.utils.arrayForEach(e.Relations, function (rel) {
                        if (rel.Role != 'Payment')
                            return;
                        var other = rel.LeftEntity == e.Name ? rel.RightEntity : rel.LeftEntity;
                        query.include(other, rel.Role);
                        _relations.push(rel);
                    });
                    filters.bind([{ Type: 3, Label: tp.get('filterby_payment_status'), Property: 'Status', EnumClass: 'NbuLibrary.Core.Domain.PaymentStatus, NbuLibrary.Core.Domain, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' }],
                          query,
                          function () { grid.reload(true); }, //filters apply callback
                          function () { grid.reload(true); });//filters clear callback
                    if (waiting)
                        filters.Filters()[0].value(1);
                    grid.bind(query, 'Finance_Customer_WaitingPayments');
                    _waitingOnly = waiting;
                });
            }
        }

        function getPaymentTarget(payment) {
            var target = null;
            ko.utils.arrayForEach(_relations, function (rel) {
                var other = rel.LeftEntity == 'Payment' ? rel.RightEntity : rel.LeftEntity;
                var key = other + '_' + rel.Role;
                if (payment.RelationsData[key])
                    target = payment.RelationsData[key];
            });
            if (target) {
                var text = tp.get('entity_' + target().Entity.Name.toLowerCase());

                var details = router.getDetailsPage(target().Entity.Name, target().Entity.Id);
                if (details) {
                    var href = details;
                    return '<a href="' + href + '">' + text + '</a>';
                }
                else
                    return text;
            }
        }


        return {
            bind: bind,
            template: 'Finance_WaitingPayments',
            Filters: filters,
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            sort: grid.sort,
            getTarget: getPaymentTarget
            //create: create
        };
    });