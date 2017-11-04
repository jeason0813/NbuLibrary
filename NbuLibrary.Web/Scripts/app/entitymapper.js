/// <reference path="../knockout-2.2.1.debug.js" />
define('entitymapper',
    ['jquery', 'ko', 'entityservice'],
    function ($, a, es) {

        var RelTypes = {
            OneToOne: 0,
            OneToMany: 1,
            ManyToOne: 2,
            ManyToMany: 3
        };

        function getRelation(key, entity) {
            var parts = key.split('_', 2);
            return ko.utils.arrayFirst(entity.Relations, function (r) { return r.Role == parts[1] && (r.RightEntity == parts[0] || r.LeftEntity == parts[0]); });
        };

        function getRelType(rel, entity) {
            if (rel.LeftEntity == entity.Name)
                return rel.Type;
            else if (rel.Type == RelTypes.OneToMany)
                return RelTypes.ManyToOne;
            else if (rel.Type == RelTypes.ManyToOne)
                return RelTypes.OneToMany;
            else return rel.Type;//one-to-one and many-to-many;
        };

        function map(entityName, raw, o) {
            return $.Deferred(function (def) {
                $.when(es.getEntity(entityName)).done(function (entity) {
                    o.Id = ko.observable(raw.Id);
                    o.Name = raw.Name;
                    o.Data = {};
                    o.RelationsData = {};
                    $.each(raw.Data, function (k, v) {
                        o.Data[k] = ko.observable(v);
                    });
                    $.each(raw.RelationsData, function (k, v) {
                        var rel = getRelation(k, entity);
                        var type = getRelType(rel, entity);
                        switch (type) {
                            case RelTypes.OneToMany:
                            case RelTypes.ManyToMany:
                                o.RelationsData[k] = ko.observableArray(v);
                                break;
                            default:
                                o.RelationsData[k] = ko.observable(v);
                        }
                    });

                    def.resolve(o);
                });
            }).promise();
        }

        function init(entityName, flds, o) {
            o.Data = {};
            o.RelationsData = {};
            return $.Deferred(function (def) {
                $.when(es.getEntity(entityName)).done(function (entity) {
                    ko.utils.arrayForEach(flds, function (field) {

                        //var prop = ko.utils.arrayFirst(entity.Properties, function (x) { return x.Name == field.Property; });
                        
                        if (!field.Entity) {
                            o.Data[field.Property] = ko.observable(); //todo: default value
                        }
                        else if (field.Entity) {
                            var key = field.Entity + '_' + field.Role;
                            var rel = getRelation(key, entity);
                            var type = getRelType(rel, entity);
                            switch (type) {
                                case RelTypes.OneToMany:
                                case RelTypes.ManyToMany:
                                    o.RelationsData[key] = ko.observableArray();
                                    break;
                                default:
                                    o.RelationsData[key] = ko.observable();
                            }
                        }
                    });
                    def.resolve(o);
                });
            }).promise();
        };

        return {
            map: map,
            init: init,
            //TODO: not suitable class for this logic - consider moving to helper
            getRelationType: getRelType
        };
    });