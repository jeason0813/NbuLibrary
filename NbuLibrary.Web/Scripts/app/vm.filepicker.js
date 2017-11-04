define('vm.filepicker',
    ['ko', 'dataservice', 'usercontext', 'filters', 'textprovider'],
    function (ko, dataservice, usercontext, gf, tp) {
        var FileAccessType =
        {
            None: 0,
            Owner: 1,
            Full: 2,
            Temporary: 3,
            Token: 4
        };

        var _files = ko.observableArray();
        var _selected = ko.observable();
        var _loaded = ko.observable(false);
        var filters = new gf.GridFilters();

        //TODO: Paging needs to be separate ui module
        var _paging = {
            Page: ko.observable(1),
            PageSize: ko.observable(10),
            HasNext: ko.observable(true),
        }
        _paging.next = function () { if (_paging.HasNext()) _paging.Page(_paging.Page() + 1); };
        _paging.prev = function () { if (_paging.HasPrev()) _paging.Page(_paging.Page() - 1); };
        _paging.HasPrev = ko.computed(function () { return _paging.Page() > 1; });

        _paging.Page.subscribe(function (val) {
            _query.setPage(val, _paging.PageSize());
            if (_loaded()) {
                load();
            }
        });

        var _query = null;

        var _bind = false;

        function bind() {
            if (!_bind) {
                _loaded(false);
                _files.removeAll();
                _query = dataservice.search('File');
                _query.addProperty('Name');
                _query.addProperty('Extension');
                var relq = _query.rules.relatedTo('User', 'Access', usercontext.currentUser().Id);
                relq.rules.relation.is('Type', FileAccessType.Owner);

                _paging.Page(1);
                _query.setPage(1, _paging.PageSize());

                filters.bind([{ Multiple: false, Type: 2, Label: tp.get('filterby_file_name'), Property: 'Name' }],
                   _query, function () { _paging.Page(1); load(); }, function () { _paging.Page(1); load(); });
                _bind = true;
            }

            load();
        }

        function load() {
            _loaded(false);
            _files.removeAll();
            $.when(_query.execute()).done(function (raw) {
                ko.utils.arrayForEach(raw, function (item) { _files.push(item); });
                _paging.HasNext(raw.length >= _paging.PageSize());
                _loaded(true);
            });
        }

        return {
            template: 'Files_ExistingFilePicker',
            bind: bind,
            Loaded: _loaded,
            Files: _files,
            Filters: filters,
            Paging : _paging,
            Selected: _selected,
            select: function (item) { _selected(item); }
        };
    });