define('mods/vm.usergroup',
    ['jquery', 'ko', 'dataservice', 'viewservice', 'entitymapper', 'entityservice', 'messanger', 'textprovider', 'grid'],
    function ($, ko, dataservice, viewservice, mapper, es, msgr, tp, entitygrid) {
        var USERGROUP = "UserGroup";
        var USER = "User";
        var MODULE_PERMISSIONS = 'ModulePermission';
        var PERMISSION_ROLE = 'Permission';

        var loadedData = ko.observable(false);

        var entity = {};
        var entityId = ko.observable();
        var _allPerms = ko.observableArray();
        var usersGrid = new entitygrid.Grid();

        function bind(id) {
            loadedData(false);
            entityId(id);

            _allPerms.removeAll();

            if (!!id) {
                var query = dataservice.get(USERGROUP, id);
                query.allProperties(true);
                query.include(USER, USERGROUP);
                query.include(MODULE_PERMISSIONS, PERMISSION_ROLE);

                var uQuery = dataservice.search(USER);
                uQuery.rules.relatedTo(USERGROUP, USERGROUP, id);
                usersGrid.bind(uQuery, 'Account_Admin_UsersGrid');
            }
            var allPermsLoad = dataservice.search(MODULE_PERMISSIONS).allProperties(true).execute();
            if (!!id) {
                $.when(query.execute())
                .done(function (data) {
                    mapper.map(USERGROUP, data, entity)
                    .done(function () {

                        $.when(allPermsLoad).done(function (perms) {

                            ko.utils.arrayForEach(perms, function (perm) {
                                var ex = null;
                                if (entity.RelationsData[MODULE_PERMISSIONS + '_' + PERMISSION_ROLE]) {
                                    ex = ko.utils.arrayFirst(entity.RelationsData[MODULE_PERMISSIONS + '_' + PERMISSION_ROLE](), function (x) { return x.Entity.Id == perm.Id; });
                                }
                                perm.Options = [];
                                ko.utils.arrayForEach(perm.Data.Available.split(';'), function (permName) {
                                    if (!permName)
                                        return;
                                    var has = false;
                                    if (ex)
                                        has = ex.Data.Granted.indexOf(permName) >= 0;

                                    perm.Options.push({ name: permName, has: ko.observable(has) });
                                });
                                _allPerms.push(perm);
                            });

                            loadedData(true);
                        });
                    });
                });
            }
            else {
                entity.Data = {
                    Name: ko.observable(),
                    UserType: ko.observable()
                };
                entity.RelationsData = {}
                $.when(allPermsLoad).done(function (perms) {

                    ko.utils.arrayForEach(perms, function (perm) {
                        perm.Options = [];
                        ko.utils.arrayForEach(perm.Data.Available.split(';'), function (permName) {
                            perm.Options.push({ name: permName, has: ko.observable(false) });
                        });
                        _allPerms.push(perm);
                    });

                    loadedData(true);
                });
            }
        };

        function save() {
            var upd = dataservice.update(USERGROUP, entityId());
            upd.set('Name', entity.Data.Name());
            upd.set('UserType', entity.Data.UserType());
            ko.utils.arrayForEach(_allPerms(), function (perm) {
                var exists = false;
                if (!!entityId() && entity.RelationsData[MODULE_PERMISSIONS + '_' + PERMISSION_ROLE])
                    exists = ko.utils.arrayFirst(entity.RelationsData[MODULE_PERMISSIONS + '_' + PERMISSION_ROLE](), function (x) { return x.Entity.Id == perm.Id; }) != null;
                else
                    exists = false;

                var granted = [];
                ko.utils.arrayForEach(perm.Options, function (opt) {
                    if (opt.has())
                        granted.push(opt.name);
                });
                if (granted.length) {
                    var rel = exists ? upd.updateRelation(MODULE_PERMISSIONS, PERMISSION_ROLE, perm.Id) : upd.attach(MODULE_PERMISSIONS, PERMISSION_ROLE, perm.Id);
                    rel.set('Granted', granted.join(';'));
                }
                else if (exists) {
                    upd.detach(MODULE_PERMISSIONS, PERMISSION_ROLE, perm.Id);
                }
            });

            $.when(upd.execute())
            .done(function (res) {
                msgr.success(tp.get('msg_saved_ok'));
                cancel();
            });
        };

        function cancel() {
            window.location = '#/account/usergroups/';
        };

        function permChanged(e) {
            if (e.added) {
                var perm = ko.dataFor($(e.added.element).parent()[0]);
                //console.log('added', perm);
                var added = ko.utils.arrayFirst(perm.Options, function (x) { return x.name == e.added.id; });
                added.has(true);
            }
            else if (e.removed) {
                var perm = ko.dataFor($(e.removed.element).parent()[0]);
                //console.log('removed', perm);
                var removed = ko.utils.arrayFirst(perm.Options, function (x) { return x.name == e.removed.id; });
                removed.has(false);
            }
        }

        return {
            template: 'Account_Admin_UserGroup',
            bind: bind,

            save: save,
            cancel: cancel,
            Users : usersGrid,
            Loaded: loadedData,
            Entity: entity,
            Permissions: _allPerms,
            permChanged: permChanged
        };
    });



