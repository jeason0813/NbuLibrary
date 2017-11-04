define('entitypage',
    ['ko', 'viewservice', 'dataservice', 'entitymapper'],
    function (ko, vs, dataservice, mapper) {

        function page() {
            var _definition = ko.observable();
            var _loaded = ko.observable(false);
            var _noAccess = ko.observable(false);
            var _loadedDef = ko.observable(false);
            var _id = ko.observable();

            var _data = {};
            var _entityName = null;
            var _query = null;


            var loadDefJob = null;

            function bind(entityName, uiDef, id) {
                _loaded(false);
                _loadedDef(false);
                _noAccess(false);

                _entityName = entityName;
                _id(id);
                loadDefJob = loadDefinition(uiDef);
                _query = dataservice.get(_entityName, _id());
            };

            function loadDefinition(uiDef) {
                if (typeof (uiDef) == 'object') {
                    _definition(uiDef);
                    _loadedDef(true);
                    return uiDef;
                }
                else
                    return $.Deferred(function (deferred) {
                        $.when(vs.get(uiDef)).done(function (uiDefRes) {
                            _definition(uiDefRes);
                            _loadedDef(true);
                            deferred.resolve(uiDefRes);
                        });
                    }).promise();
            };

            function loadData() {
                return $.Deferred(function (deferred) {
                    $.when(loadDefJob).done(function () {
                        ko.utils.arrayForEach(_definition().Fields, function (fld) {
                            if (fld.Entity && fld.Role)
                                _query.include(fld.Entity, fld.Role);
                            else
                                _query.addProperty(fld.Property);
                        });

                        $.when(_query.execute())
                        .done(function (data) {
                            if (data) {
                                _noAccess(false);
                                $.when(mapper.map(_entityName, data, _data))
                                .done(function () {
                                    _loaded(true);
                                    deferred.resolve(_data);
                                });
                            }
                            else //no access
                                _noAccess(true);
                        });
                    });
                }).promise();
            };

            function loadDefaults() {
                return $.Deferred(function (deferred) {
                    $.when(loadDefJob).done(function () {
                        $.when(mapper.init(_entityName, _definition().Fields, _data))
                        .done(function () {
                            _loaded(true);
                            deferred.resolve(_data);
                        });
                    });
                }).promise();
            };

            return {
                Definition: _definition,
                Loaded: _loaded,
                LoadedDef: _loadedDef,
                Entity: _data,
                load: loadData,
                NoAccess: _noAccess,
                loadDefaults: loadDefaults,
                bind: bind,
                getQuery: function () { return _query; }
            };
        }

        return {
            Page: page
        };
    });