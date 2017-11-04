define('mods/vm.mysubscriptions',
    ['jquery', 'ko', 'textprovider', 'dataservice', 'datacontext', 'messanger', 'grid', 'usercontext', 'entitypage', 'presenter', 'filters'],
    function ($, ko, tp, ds, dc, msgr, entitygrid, usercontext, ep, presenter, gf) {

        var query = null;
        var specialRelQuery = null;
        var template = ko.observable();
        var grid = new entitygrid.Grid();
        var filters = new gf.GridFilters();
        var _id = ko.observable();
        var _all = ko.observable();
        var _customerName = ko.observable();
        var _onlyActiveSubscriptions = ko.observable(false);

        _onlyActiveSubscriptions.subscribe(function (nv) {
            if (usercontext.isLibrarian()) {
                if (!!nv) {
                    specialRelQuery = query.rules.relatedTo('User', 'Subscriber', _id);
                    specialRelQuery.rules.relation.is('IsActive', true);
                    grid.reload();
                }
                else if (specialRelQuery) {
                    query.rules.remove(specialRelQuery);
                    grid.reload();
                }
            }
        });

        function bind(id, all) {
            if (_id() === id && _all() === all)
                grid.reload();
            else {
                _id(id);
                _all(all);
                _customerName('');

                query = ds.search('Magazine');
                query.include('User', 'Subscriber');
                //query.rules.is('IsActive', true);

                var filterDefs = [{ Multiple: true, Entity: 'MagazineCategory', Role: 'Category', Formula: '{Value}', Type: 4, Label: tp.get('filterby_magazinecategory') },
                    { Multiple: false, Type: 7, Property: 'Title', Label: tp.get('filterby_magazinetitle') }];
                if (!all) {
                    var relQuery = query.rules.relatedTo('User', 'Subscriber', id);
                    relQuery.rules.relation.is('IsActive', true);
                    uiDef = 'MyMagazines_Customer_SubscriptionsGrid';
                    template('MyMagazines_Customer_Subscriptions');
                }
                else {
                    uiDef = 'MyMagazines_Customer_MagazinesGrid';
                    template('MyMagazines_Customer_MagazinesGrid');
                }

                filters.bind(filterDefs,
                    query,
                    function () { grid.reload(true); }, //filters apply callback
                    function () { grid.reload(true); });//filters clear callback

                query.sort('Title');
                grid.bind(query, uiDef);
                if (usercontext.isLibrarian() && id) {
                    var custQ = ds.get('User', id);
                    custQ.addProperty("FullName");
                    custQ.addProperty("Email");
                    $.when(custQ.execute()).done(function (cust) {
                        _customerName(cust.Data.FullName + ' (' + cust.Data.Email + ')');
                    });
                }
            }
        };


        function isActive(magazine) {
            if (!magazine.RelationsData.User_Subscriber)
                return false;

            var subscription = ko.utils.arrayFirst(magazine.RelationsData.User_Subscriber(), function (s) { return s.Entity.Id == _id(); });
            if (subscription)
                return subscription.Data.IsActive;
            else
                return false;
        };

        function hasRelation(magazine) {
            return magazine.RelationsData.User_Subscriber
                && magazine.RelationsData.User_Subscriber()
                && ko.utils.arrayFirst(magazine.RelationsData.User_Subscriber(), function (s) { return s.Entity.Id == _id(); });
        }

        function setActive(magazine, value) {
            var update = ds.update('User', _id());
            var relUpdate = null
            if (hasRelation(magazine))
                relUpdate = update.updateRelation('Magazine', 'Subscriber', magazine.Id);
            else
                relUpdate = update.attach('Magazine', 'Subscriber', magazine.Id);

            relUpdate.set('IsActive', value);
            $.when(update.execute()).done(function () {
                msgr.success(tp.get('msg_saved_ok'));
                grid.reload();
            });
        };

        function setActive_Librarian(magazine, value) {
            var update = ds.update('Magazine', magazine.Id);
            var relUpdate = null
            if (hasRelation(magazine))
                relUpdate = update.updateRelation('User', 'Subscriber', _id());
            else
                relUpdate = update.attach('User', 'Subscriber', _id());

            relUpdate.set('IsActive', value);
            $.when(update.execute()).done(function () {
                msgr.success(tp.get('msg_saved_ok'));
                grid.reload();
            });
        };


        return {
            bind: bind,
            template: template,
            Customer: _customerName,
            IsLibrarian: usercontext.isLibrarian(),
            Items: grid.Items,
            Definition: grid.Definition,
            LoadedDef: grid.LoadedDef,
            Loaded: grid.Loaded,
            Sorting: grid.Sorting,
            Paging: grid.Paging,
            Filters: filters,
            OnlyActiveSubscriptions: _onlyActiveSubscriptions,
            sort: grid.sort,
            isActive: isActive,
            activate: function (mag) { usercontext.isCustomer() ? setActive(mag, true) : setActive_Librarian(mag, true); },
            deactivate: function (mag) { usercontext.isCustomer() ? setActive(mag, false) : setActive_Librarian(mag, false); }
            //canEdit: canEdit,
            //canCancel: canEdit,
            //process: process,
            //cancel: cancel,
            //create: create,
            //viewNotifications: viewNotifications
        };
    });




