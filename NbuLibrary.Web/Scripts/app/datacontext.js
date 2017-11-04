define('datacontext',
    ['jquery', 'ko', 'entityservice', 'entitymapper', 'dataservice'],
    function ($, ko, es, em, dataservice) {

        //enums
        var RelationOperation = {
            None: 0,
            Attach: 1,
            Detach: 2
        };

        var RelTypes = {
            OneToOne: 0,
            OneToMany: 1,
            ManyToOne: 2,
            ManyToMany: 3
        };

        var _entity = {
            Data: {},
            RelationsData: {}
        };
        var _id = null;
        var _entityName = null;

        var _entityLoaded = null;
        var _relChanges = [];
        var _current = {
            Data: {},
            RelationsData: {}
        };
        var _changes = [];

        var _hasChanges = ko.observable(false);

        function bindContext(entityName, entity, id) {
            clear();
            _id = id;
            _entityName = entityName;
            _entityLoaded = es.getEntity(entityName);
            //$.when(_entityLoaded).done(function (entity) {
            $.each(entity.Data, function (k, v) {
                _current.Data[k] = v();
                if (ko.isObservable(v))
                    _entity.Data[k] = v;
                else
                    throw Error(k);
            });
            $.each(entity.RelationsData, function (k, v) {
                _current.RelationsData[k] = v();
                if (ko.isObservable(v))
                    _entity.RelationsData[k] = v;
                else
                    throw Error(k);
            });
            //});
        };

        //function setRelations(entityName, role, ids) {
        //    _relCurrent[entityName + '_' + role] = {};
        //    ko.utils.arrayForEach(ids, function (id) {
        //        _relCurrent[entityName + '_' + role][id] = true;
        //    });
        //}

        function addRelation(en, role, id) {
            //todo: optimize
            $.when(_entityLoaded).done(function (entity) {
                if (_entity.RelationsData[en + '_' + role]) {
                    var prop = _entity.RelationsData[en + '_' + role];
                    if (isObservableArray(prop))
                        prop.push({ Data: {}, Entity: { Id: id } });
                    else
                        prop({ Data: {}, Entity: { Id: id } });
                }
                var rel = ko.utils.arrayFirst(entity.Relations, function (r) { return r.Role == role; });
                var relType = em.getRelationType(rel, entity);
                var toOne = relType == RelTypes.OneToOne || relType == RelTypes.ManyToOne;
                if (toOne) {
                    var other = ko.utils.arrayFirst(_relChanges, function (x) { return x.Entity == en && x.Role == role && x.Operation == RelationOperation.Attach && x.Id != id });
                    if (other)
                        ko.utils.arrayRemoveItem(_relChanges, other);

                    if (_current.RelationsData[en + '_' + role] && _current.RelationsData[en + '_' + role].Entity.Id != id)
                        removeRelation(en, role, _current.RelationsData[en + '_' + role].Entity.Id);
                }

                //todo:optimize
                var ex = ko.utils.arrayFirst(_relChanges, function (x) { return x.Entity == en && x.Role == role && x.Id == id });
                if (ex) {
                    if (ex.Operation == RelationOperation.Attach) {
                        _hasChanges(hasChanges());
                        return;
                    }
                    else if (ex.Operation == RelationOperation.Detach) {
                        ko.utils.arrayRemoveItem(_relChanges, ex);
                        _hasChanges(hasChanges());
                        return;
                    }
                }

                var persisted = null;
                if (toOne)
                    persisted = _current.RelationsData[en + '_' + role] && _current.RelationsData[en + '_' + role].Entity.Id == id;
                else if (_current.RelationsData[en + '_' + role])
                    persisted = ko.utils.arrayFirst(_current.RelationsData[en + '_' + role], function (x) { return x.Entity.Id == id; });

                if (!persisted)
                    _relChanges.push({ Entity: en, Role: role, Id: id, Operation: RelationOperation.Attach });

                _hasChanges(hasChanges());
            });
        };

        function removeRelation(en, role, id) {
            var ex = ko.utils.arrayFirst(_relChanges, function (x) { return x.Entity == en && x.Role == role && x.Id == id });
            if (ex) {
                if (ex.Operation == RelationOperation.Detach)
                    return;
                else if (ex.Operation == RelationOperation.Attach)
                    ko.utils.arrayRemoveItem(_relChanges, ex);
            }
            else
                _relChanges.push({ Entity: en, Role: role, Id: id, Operation: RelationOperation.Detach });

            _hasChanges(hasChanges())
        };

        function set(prop, val) {
            var change = ko.utils.arrayFirst(_changes, function (c) { return c.Property == prop; });
            if (_entity.Data[prop])
                _entity.Data[prop](val);
            if (_current.Data[prop] === val || _current.Data[prop] === parseInt(val) || _current.Data[prop] === parseFloat(val)) {
                if (change)
                    ko.utils.arrayRemoveItem(_changes, change);
            }
            else if (!change)
                _changes.push({ Property: prop, Value: val });

            else if (change && change.Value != val)
                change.Value = val;

            _hasChanges(hasChanges())
        }

        function clear() {
            _hasChanges(false);
            while (_relChanges.length)
                _relChanges.pop();

            while (_changes.length)
                _changes.pop();

            _hasChanges(hasChanges())
        };

        function getChanges() {
            return _relChanges;
        };

        function applyChanges(update) {
            ko.utils.arrayForEach(_changes, function (change) {
                update.set(change.Property, change.Value);
            });
            ko.utils.arrayForEach(_relChanges, function (change) {
                if (change.Operation == RelationOperation.Attach)
                    update.attach(change.Entity, change.Role, change.Id);
                else if (change.Operation == RelationOperation.Detach)
                    update.detach(change.Entity, change.Role, change.Id);
            });
        };

        function hasChanges() {
            return !!(_changes.length || _relChanges.length);
        };

        function save() {
            if (hasChanges()) {
                var update = dataservice.update(_entityName, _id);
                applyChanges(update);
                return update.execute();
            }
        };

        function isObservableArray(o) {
            return typeof (o) === 'function' && o.name === 'observableArray';
        }

        return {
            bind: bindContext,
            clear: clear,
            removeRelation: removeRelation,
            addRelation: addRelation,
            set: set,
            getChanges: getChanges,
            getEntity: function () { return _entity; },
            applyChanges: applyChanges,
            hasChanges: _hasChanges,
            save: save
        };
    });