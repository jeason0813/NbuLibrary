//TODO: Make grid helper module to be used with multiple instances

define('grid',
     ['jquery', 'ko', 'textprovider', 'dataservice', 'viewservice', 'entitymapper'],
    function ($, ko, tp, ds, vs, entitymapper) {

        var ViewTypes = {
            Textfield: 0,
            Numberfield: 1,
            Datefield: 2,
            Enumfield: 3,
            Checkfield: 4,
            Listfield: 5,
            Filefield: 6
        };

        //var loaded = vs.get('Account_Admin_UsersGrid');

        function grid() {
            var _loadView = null;
            var _items = ko.observableArray([]);
            var _definition = ko.observable();
            var _loadedDef = ko.observable(false);
            var _loadedItems = ko.observable(false);
            var _sortBy = ko.observable();
            var _sorting = {
                Field: ko.observable(),
                Desc: ko.observable()
            }

            var _paging = {
                Total: ko.observable(),
                Page: ko.observable(1),
                PageSize: ko.observable(10),
                QuickPages: ko.observableArray()
            }
            _paging.goToFirst = function () { if (_paging.HasPrev()) _paging.Page(1); };
            _paging.goToLast = function () {
                if (_paging.HasNext()) {
                    var lastPage = Math.ceil(_paging.Total() / _paging.PageSize());
                    _paging.Page(lastPage);
                }
            };
            _paging.next = function () { if (_paging.HasNext()) _paging.Page(_paging.Page() + 1); };
            _paging.prev = function () { if (_paging.HasPrev()) _paging.Page(_paging.Page() - 1); };
            _paging.From = ko.computed(function () { return (_paging.Page() - 1) * _paging.PageSize() + 1; });
            _paging.To = ko.computed(function () { return Math.min(_paging.Total(), _paging.Page() * _paging.PageSize()); });

            _paging.HasPrev = ko.computed(function () { return _paging.Page() > 1; });
            _paging.HasNext = ko.computed(function () { return _paging.To() < _paging.Total(); });
            _paging.computeQuickPages = function () {
                var qp = [];
                var totalPages = Math.ceil(_paging.Total() / _paging.PageSize());
                var startPage = Math.max(1, _paging.Page() - 2);
                var endPage = Math.min(totalPages, _paging.Page() + 4 - (_paging.Page() - startPage));
                startPage = Math.max(1, _paging.Page() - 4 + (endPage - _paging.Page()));
                for (var p = startPage; p <= endPage; p++) {
                    qp.push(p);
                }
                _paging.QuickPages(qp);
            };

            _paging.jump = function (page) {
                if (page != _paging.Page())
                    _paging.Page(page);
            };

            _paging.Page.subscribe(function (val) {
                if (_loadedItems()) {
                    _query.setPage(val, _paging.PageSize());
                    load();
                    _paging.computeQuickPages();
                }
            });

            var _query = null;

            var _template = ko.observable();

            function load(resetPaging) {
                _loadedItems(false);
                if (resetPaging) {
                    _paging.Page(1);
                    _paging.Total(0);
                    _query.setPage(_paging.Page(), _paging.PageSize());
                }

                if (!_paging.Total()) {
                    $.when(_query.executeCount()).done(function (cnt) {
                        _paging.Total(cnt);
                        _paging.computeQuickPages();
                    });
                }

                _items.removeAll();
                $.when(_query.execute())
                    .done(function (rawData) {
                        var jobs = [];
                        $(rawData).each(function () {
                            var e = {};
                            _items.push(e);
                            jobs.push(entitymapper.map(this.Name, this, e));
                        });
                        $.when.apply($, jobs).done(function () {
                            _loadedItems(true);
                        });
                    });
            };


            function bind(query, viewDef, template) {
                _loadedDef(false);
                _loadedItems(false);
                //todo
                _template(template);

                _query = query;
                _paging.Page(1);
                _query.setPage(1, _paging.PageSize());
                if (typeof (viewDef) == 'object')
                    _loadView = viewDef;
                else
                    _loadView = vs.get(viewDef);
                $.when(_loadView).done(function (view) {
                    _definition(view);
                    _loadedDef(true);
                    $(_definition().Fields).each(function () {
                        if (this.Entity && this.Role)
                            query.include(this.Entity, this.Role);
                        else
                            query.addProperty(this.Property);
                    });

                    load();
                });
            };

            function sort(field) {
                if (field.Type == ViewTypes.Listfield || field.Type == ViewTypes.Filefield)
                    return;

                if (_sorting.Field() === field) {
                    _sorting.Desc(!_sorting.Desc());
                    _query.sort(field.Property, true, field.Role, field.Entity);
                }
                else {
                    _sorting.Field(field);
                    _sorting.Desc(false);
                }

                _query.sort(_sorting.Field().Property, _sorting.Desc(), field.Role, field.Entity);

                load();
            };

            return {
                bind: bind,
                template: _template,
                Items: _items,
                Definition: _definition,
                LoadedDef: _loadedDef,
                Loaded: _loadedItems,
                Sorting: _sorting,
                Paging: _paging,
                //remove: remove,
                sort: sort,
                reload: load
            };
        }

        //function remove(user) {
        //    $.when(ds.remove(USER, user.Id(), false))
        //    .done(function () {
        //        items.remove(user);
        //    });
        //};

        return {
            Grid: grid
        };
    });