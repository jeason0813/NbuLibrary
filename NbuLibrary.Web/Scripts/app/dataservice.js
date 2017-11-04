/// <reference path="../lib/RequireJS/require.debug.js" />
/// <reference path="../lib/jQuery/jquery-1.9.1.js" />

define('dataservice',
    ['jquery', 'presenter', 'textprovider', 'messanger'],
    function (jQuery, presenter, tp, msgr) {

        var RelationOperation = {
            None: 0,
            Attach: 1,
            Detach: 2
        };

        var root = '/api/';

        function search(entity) {
            var query = (function () {
                var _data = {
                    Entity: entity,
                    Properties: [],
                    AllProperties: false,
                    Rules: [],
                    Includes: [],
                    RelatedTo: [],
                    SortBy: {},
                    Paging: {}
                };

                //TODO: Rules
                function addRuleIS(prop, val) {
                    if (prop) {
                        var rule = { Property: prop, Values: [val], Operator: 'is' };
                        _data.Rules.push(rule);
                        return rule;
                    }
                };

                function addRuleAnyOf(prop, vals) {
                    if (prop) {
                        var rule = { Property: prop, Values: vals, Operator: 'anyof' };
                        _data.Rules.push(rule);
                        return rule;
                    }
                }

                function addRuleStartsWith(prop, val) {
                    if (prop) {
                        var rule = { Property: prop, Values: [val], Operator: 'sw' };
                        _data.Rules.push(rule);
                        return rule;
                    }
                }

                function addRuleContainsPhrase(prop, val) {
                    if (prop) {
                        var rule = { Property: prop, Values: [val], Operator: 'cntp' };
                        _data.Rules.push(rule);
                        return rule;
                    }
                }

                function addRelRule(entity, role) {
                    //var multiple = !!id.length;
                    var relQuery = {
                        Entity: entity,
                        Role: role,
                        Properties: [],
                        AllProperties: false,
                        Rules: [], //[{ Property: 'id', Values: multiple ? id : [id], Operator: multiple ? 'anyof' : 'is' }],
                        RelationRules: [],
                        Includes: [],
                        RelatedTo: []
                    };
                    _data.RelatedTo.push(relQuery);
                    relQuery.rules = {
                        is: function (prop, val) {
                            if (prop)
                                relQuery.Rules.push({ Property: prop, Values: [val], Operator: 'is' });
                        },
                        startsWith: function (prop, val) {
                            if (prop) {
                                var rule = { Property: prop, Values: [val], Operator: 'sw' };
                                relQuery.Rules.push(rule);
                                return rule;
                            }
                        },
                        anyOf: function (prop, vals) {
                            if (prop) {
                                var rule = { Property: prop, Values: vals, Operator: 'anyof' };
                                relQuery.Rules.push(rule);
                                return rule;
                            }
                        },
                        relation: {
                            is: function (prop, val) {
                                if (prop)
                                    relQuery.RelationRules.push({ Property: prop, Values: [val], Operator: 'is' });
                            }
                        },
                        remove: function (rule) { ko.utils.arrayRemoveItem(relQuery.Rules, rule); }
                    }
                    return relQuery;
                };


                return {
                    execute: function () {
                        return $.Deferred(function (def) {
                            $.ajax({
                                type: 'POST',
                                url: root + 'Entity/Search',
                                data: _data,
                                dataType: 'json',
                                success: function (entities) {
                                    //console.log(entities);
                                    def.resolve(entities);
                                }
                            });
                        }).promise();
                    },
                    executeCount: function () {
                        return $.Deferred(function (def) {
                            $.ajax({
                                type: 'POST',
                                url: root + 'Entity/Count',
                                data: _data,
                                dataType: 'json',
                                success: function (result) {
                                    //console.log(entities);
                                    def.resolve(result);
                                }
                            });
                        }).promise();
                    },
                    getRequest: function () { return _data; },
                    allProperties: function (v) {
                        _data.AllProperties = !!v;
                        return this;
                    },
                    setPage: function (page, pageSize) {
                        _data.Paging = { Page: page, PageSize: pageSize };
                    },
                    include: function (entity, role) {
                        if (!ko.utils.arrayFirst(_data.Includes, function (inc) { return inc.Entity == entity && inc.Role == role; }))
                            _data.Includes.push({ Entity: entity, Role: role });
                    },
                    addProperty: function (prop) {
                        if (ko.utils.arrayFirst(_data.Properties, function (p) {
                            return p.toLowerCase() == prop.toLowerCase();
                        }) == null) {
                            _data.Properties.push(prop);
                        }
                    },
                    sort: function (prop, desc, role, entity) {
                        _data.SortBy = { Property: prop, Descending: desc || false, Role: role, Entity: entity };
                    },
                    rules: {
                        is: addRuleIS,
                        anyOf: addRuleAnyOf,
                        startsWith: addRuleStartsWith,
                        containsPhrase: addRuleContainsPhrase,
                        lessThan: function (prop, val) {
                            if (prop) {
                                var rule = { Property: prop, Values: [val], Operator: 'lte' };
                                _data.Rules.push(rule);
                                return rule;
                            }
                        },
                        greaterThan: function (prop, val) {
                            if (prop) {
                                var rule = { Property: prop, Values: [val], Operator: 'gte' };
                                _data.Rules.push(rule);
                                return rule;
                            }
                        },
                        relatedTo: function (entity, role, id) {
                            var rr = addRelRule(entity, role);
                            var multiple = id instanceof Array && id.length;
                            if (!multiple)
                                rr.rules.is('id', id);
                            else
                                rr.rules.anyOf('id', id);
                            return rr;
                        },
                        relatedToBlank: function (entity, role) {
                            return addRelRule(entity, role);
                        },
                        remove: function (rule) {
                            if (rule.Rules)
                                ko.utils.arrayRemoveItem(_data.RelatedTo, rule);
                            else
                                ko.utils.arrayRemoveItem(_data.Rules, rule);
                        }
                    }
                };

            })();

            return query;
        };

        function get(entity, id) {
            var query = search(entity);
            query.rules.is("id", id);
            var exec = query.execute;
            query.execute = function () {
                return $.Deferred(function (def) {
                    $.when(exec()).done(function (res) {
                        if (res)
                            def.resolve(res.length ? res[0] : null);
                        else
                            def.resolve();
                    }).promise();
                });
            }
            return query;
        };

        function update(entityName, id) {
            var cmd = (function () {
                var _data = {
                    Id: id,
                    Entity: entityName,
                    PropertyUpdates: [],
                    RelationUpdates: []
                };

                function addPropUpdate(propName, value) {
                    _data.PropertyUpdates.push({ Name: propName, Value: value }); //TODO: set the same property again
                };

                function addRelation(relEntity, role, relId) {
                    var relUpdate = {
                        Id: relId,
                        Entity: relEntity,
                        Role: role,
                        PropertyUpdates: [],
                        RelationUpdates: [],
                        Operation: RelationOperation.Attach
                    };
                    _data.RelationUpdates.push(relUpdate);

                    return {
                        set: function (prop, val) {
                            relUpdate.PropertyUpdates.push({ Name: prop, Value: val }); //TODO: set the same property again
                        }
                    };
                };

                function updateRelation(relEntity, role, relId) {
                    var relUpdate = {
                        Id: relId,
                        Entity: relEntity,
                        Role: role,
                        PropertyUpdates: [],
                        RelationUpdates: [],
                        Operation: RelationOperation.None
                    };
                    _data.RelationUpdates.push(relUpdate);

                    return {
                        set: function (prop, val) {
                            relUpdate.PropertyUpdates.push({ Name: prop, Value: val }); //TODO: set the same property again
                        }
                    };
                };

                function removeRelation(relEntity, role, relId) {
                    _data.RelationUpdates.push({
                        Id: relId,
                        Entity: relEntity,
                        Role: role,
                        PropertyUpdates: {},
                        RelationUpdates: [],
                        Operation: RelationOperation.Detach
                    });
                };

                return {
                    set: addPropUpdate,
                    attach: addRelation,
                    detach: removeRelation,
                    updateRelation: updateRelation,
                    getRelationUpdate: function (relEntity, role, relId) {
                        var relUpdate = ko.utils.arrayFirst(_data.RelationUpdates, function (ru) { return ru.Id === relId && ru.Entity === relEntity && ru.Role === role });
                        if (relUpdate) {
                            return {
                                set: function (prop, val) {
                                    relUpdate.PropertyUpdates.push({ Name: prop, Value: val }); //TODO: set the same property again
                                }
                            };
                        }
                    },
                    execute: function () {
                        //console.log('executing update', _data);

                        return $.Deferred(function (def) {
                            $.ajax({
                                type: 'POST',
                                url: '/api/Entity/Update',
                                data: _data,
                                dataType: 'json',
                                success: function (response) {
                                    //console.log('update', response);
                                    if (response.Success)
                                        def.resolve(response);
                                    else if (response.Errors) {
                                        def.reject();
                                        $(response.Errors).each(function () {
                                            msgr.error(this.Message);
                                        });
                                    }
                                }
                            });
                        });
                    }
                };
            })();

            return cmd;
        }

        function remove(entity, id, recursive) {
            return $.Deferred(function (def) {

                $.ajax({
                    type: 'POST',
                    url: '/api/Entity/Delete',
                    data: { Entity: entity, Id: id, Recursive: recursive },
                    dataType: 'json',
                    success: function (response) {
                        if (response.Success)
                            def.resolve();
                        else if (response.Errors && response.Errors.length == 1 && response.Errors[0].ErrorType == 'RelationExists' && !recursive) {
                            var ask = presenter.ask(tp.get('confirm_entity_deletion_recursive'));
                            ask.yes(function () {
                                $.when(remove(entity, id, true))
                                .done(function (r) {
                                    def.resolve(r)
                                });
                            });
                            ask.show();
                        }
                        else if (response.Errors) {
                            def.reject();
                            $(response.Errors).each(function () {
                                msgr.error(this.Message);
                            });
                        }
                        else
                            def.reject(response);
                    },
                    error: this.errorHandler
                });
            }).promise();
        };

        return {
            get: get,
            search: search,
            remove: remove,
            update: update
        };
    });