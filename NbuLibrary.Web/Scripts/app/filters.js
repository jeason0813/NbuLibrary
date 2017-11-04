/// <reference path="../knockout-2.3.0.debug.js" />

define('filters',
    ['dataservice'],
    function (dataservice) {

        var FilterTypes = {
            StartsWith: 1,
            ContainsPhrase: 2,
            EnumValue: 3,
            Relation: 4,
            Boolean: 5,
            PropertyValue: 6,
            Search: 7,
            RelationStartsWith: 8,
            Date: 9
        };

        function GridFilters() {

            var _query = null;
            var _filters = ko.observableArray();
            var _applyCallback = null;
            var _clearCallback = null;
            function bind(filtersDef, query, applyCallback, clearCallback) {
                _filters.removeAll();
                _query = query;
                _applyCallback = applyCallback;
                _clearCallback = clearCallback;
                ko.utils.arrayForEach(filtersDef, function (fd) {
                    var filter = {
                        value: fd.Multiple ? ko.observableArray() : ko.observable(),
                        definition: fd,
                        rule: null
                    };
                    filter.clear = function () {
                        if (filter.rule)
                            query.rules.remove(filter.rule);
                        if (fd.Multiple)
                            filter.value.removeAll();
                        else
                            filter.value(null);
                    }

                    switch (fd.Type) {
                        case FilterTypes.Relation:
                            filter.onChange = function (e) {
                                if (e.added) {
                                    if (filter.rule != null)
                                        _query.rules.remove(filter.rule);
                                    var rel = { Entity: { Name: fd.Entity, Id: e.added.id } };
                                    if (fd.Multiple)
                                        filter.value.push(rel);
                                    else
                                        filter.value(rel);
                                    var ruleValue = null;
                                    if (fd.Multiple) {
                                        ruleValue = [];
                                        ko.utils.arrayForEach(filter.value(), function (r) { ruleValue.push(r.Entity.Id); });
                                    }
                                    else
                                        ruleValue = rel.Entity.Id;

                                    filter.rule = _query.rules.relatedTo(fd.Entity, fd.Role, ruleValue);
                                }
                                else if (e.removed) {
                                    query.rules.remove(filter.rule);
                                    if (fd.Multiple) {
                                        filter.value.remove(function (r) { return r.Entity.Id == e.removed.id; });
                                        if (filter.value().length) {
                                            var ruleValue = [];
                                            ko.utils.arrayForEach(filter.value(), function (r) { ruleValue.push(r.Entity.Id); });
                                            filter.rule = _query.rules.relatedTo(fd.Entity, fd.Role, ruleValue);
                                        }
                                    }
                                    else
                                        filter.value(null);
                                }
                            }
                            break;
                        case FilterTypes.RelationStartsWith:
                            filter.value.subscribe(function (newValue) {
                                if (filter.rule != null)
                                    query.rules.remove(filter.rule);
                                if (newValue) {
                                    filter.rule = query.rules.relatedToBlank(fd.Entity, fd.Role);
                                    filter.rule.rules.startsWith(fd.Property, newValue)
                                }
                            });
                            break;
                        case FilterTypes.StartsWith:
                            filter.value.subscribe(function (newValue) {
                                if (filter.rule != null)
                                    query.rules.remove(filter.rule);
                                if (newValue)
                                    filter.rule = query.rules.startsWith(fd.Property, newValue);
                            });
                            break;
                        case FilterTypes.ContainsPhrase:
                        case FilterTypes.Search://TODO: search (Fulltext index)
                            filter.value.subscribe(function (newValue) {
                                if (filter.rule != null)
                                    query.rules.remove(filter.rule);
                                if (newValue)
                                    filter.rule = query.rules.containsPhrase(fd.Property, newValue);
                            });
                            break;
                        case FilterTypes.EnumValue:
                            filter.value.subscribe(function (newValue) {
                                if (filter.rule != null)
                                    query.rules.remove(filter.rule);
                                if (newValue) {
                                    if (fd.Multiple && newValue.length > 0)
                                        filter.rule = query.rules.anyOf(fd.Property, newValue);
                                    else if (!fd.Multiple)
                                        filter.rule = query.rules.is(fd.Property, newValue);
                                }
                            });
                            break;
                        case FilterTypes.Boolean:
                            filter.onChange = function (e) {
                                if (e.added) {
                                    if (filter.rule != null)
                                        _query.rules.remove(filter.rule);
                                    filter.value(e.added.id);
                                    if (!fd.Entity || !fd.Role) {
                                        filter.rule = _query.rules.is(fd.Property, e.added.id == 2 ? true : false);
                                    }
                                    else {
                                        var relTo = ko.utils.arrayFirst(_query.getRequest().RelatedTo, function (x) { return x.Entity == fd.Entity && x.ROle == fd.Role });
                                        if (relTo) {
                                            relTo.rules.is(fd.Property, e.added.id == 2 ? true : false);
                                        }
                                    }
                                }
                                else if (e.removed) {
                                    if (!fd.Entity || !fd.Role) {
                                        query.rules.remove(filter.rule);
                                    }
                                    else {
                                        var relTo = ko.utils.arrayFirst(_query.getRequest().RelatedTo, function (x) { return x.Entity == fd.Entity && x.ROle == fd.Role });
                                        if (relTo) {
                                            relTo.rules.remove(filter.rule);
                                        }
                                    }
                                }
                            };
                            break;
                        case FilterTypes.PropertyValue:
                            filter.value.subscribe(function (newValue) {
                                if (filter.rule != null)
                                    query.rules.remove(filter.rule);
                                if (newValue)
                                    filter.rule = query.rules.is(fd.Property, newValue);
                            });
                            filter.options = ko.observableArray();
                            (function () {
                                var rq = query.getRequest();

                                var q = new dataservice.search(rq.Entity);
                                ko.utils.arrayForEach(rq.Rules, function (r) { q.getRequest().Rules.push(r); })
                                ko.utils.arrayForEach(rq.RelatedTo, function (r) { q.getRequest().RelatedTo.push(r); })
                                q.addProperty(fd.Property);
                                $.when(q.execute()).done(function (res) {
                                    var map = {};
                                    ko.utils.arrayForEach(res, function (e) {
                                        var value = e.Data[fd.Property];
                                        if (!map[value]) {
                                            map[value] = true;
                                            filter.options.push(value);
                                        }
                                    });
                                });
                            })();
                            break;
                        case FilterTypes.Date:
                            filter.value.subscribe(function (newValue) {
                                if (filter.rule != null)
                                    query.rules.remove(filter.rule);
                                if (newValue) {
                                    if (fd.Before)
                                        filter.rule = query.rules.lessThan(fd.Property, newValue);
                                    else if(fd.After)
                                        filter.rule = query.rules.greaterThan(fd.Property, newValue);
                                    else
                                        filter.rule = query.rules.is(fd.Property, newValue);
                                }
                            });
                            break;
                            break;
                        default:
                            throw new Error("Filter of type " + fd.Type + " not implemented yet.");
                    }

                    _filters.push(filter);
                });
            }

            function applyFilters() {
                _applyCallback();
            }

            function clearFilters() {
                ko.utils.arrayForEach(_filters(), function (f) {
                    f.clear();
                });
                _clearCallback();
            }

            return {
                bind: bind,
                apply: applyFilters,
                clearAll: clearFilters,
                Filters: _filters
            };
        };

        return {
            GridFilters: GridFilters
        };
    });