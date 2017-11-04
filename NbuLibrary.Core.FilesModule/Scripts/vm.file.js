define('mods/vm.file',
    ['jquery', 'usercontext', 'entitymapper', 'dataservice', 'textprovider', 'presenter'],
    function ($, usercontext, mapper, dataservice, tp, presenter) {

        var MODULE_ID = 4;
        var Permissions = {
            ManageOwn: 'ManageOwn',
            ViewOwn: 'ViewOwn',
            ManageAll: 'ManageAll'
        };

        var FileAccessType =
        {
            None: 0,
            Owner: 1,
            Full: 2,
            Temporary: 3,
            Token: 4
        }

        var ViewTypes = {
            Textfield: 0,
            Numberfield: 1,
            Datefield: 2,
            Enumfield: 3,
            Checkfield: 4
        };
        var _loaded = ko.observable(false);
        var _file = {};
        var _id = ko.observable();
        function bind(id) {
            _loaded(false);
            _id(id);
            load();
        }

        function load() {
            _loaded(false);
            var q = dataservice.get('File', _id()).allProperties(true);
            q.include('User', 'Access');
            $.when(q.execute()).done(function (e) {
                $.when(mapper.map('File', e, _file)).done(function () {
                    ko.utils.arrayForEach(_file.RelationsData.User_Access(), function (ac) {
                        ac.Original = {
                            Type: ko.observable(ac.Data.Type),
                            Expire: ko.observable(ac.Data.Expire)
                        };
                        ac.Data.Type = ko.observable(ac.Data.Type);
                        ac.Data.Expire = ko.observable(ac.Data.Expire);
                        ac.HasChanges = ko.computed(function () {
                            return ac.Data.Type() != ac.Original.Type() || ac.Data.Expire() != ac.Original.Expire();
                        });
                    });
                    _loaded(true);
                });
            });
        }

        function addNewPerm() {
            var vm = (function () {

                var _selectedUser = ko.observable();
                var _selectedPerm = ko.observable();
                var _expire = ko.observable();

                return {
                    template: 'Files_ManageFile_GrantPopup',

                    UsersAjax: {
                        url: "api/entity/search",
                        type: 'post',
                        dataType: 'json',
                        data: function (term, page) {
                            var q = dataservice.search('User');
                            q.allProperties(true);//todo: all props
                            q.rules.startsWith('FullName', term);
                            q.setPage(page, 10);
                            return q.getRequest();
                        },
                        results: function (data, page) {
                            var more = data.length == 10; // whether or not there are more results available

                            var res = [];
                            ko.utils.arrayForEach(data, function (item) {
                                res.push({ text: item.Data.FullName, id: item.Id });
                            });
                            // notice we return the value of more so Select2 knows if more results can be loaded
                            return { results: res };
                        }
                    },
                    Permission: _selectedPerm,
                    User: _selectedUser,
                    Expire: _expire,
                    onChangeUser: function (e) {
                        if (e.added)
                            _selectedUser(e.added.id);
                        else
                            _selectedUser(null);
                    }
                };
            })();


            var popup = presenter.popup(vm, {title: tp.get('heading_file_grantperm')});
            popup.ok(tp.get('btn_grant_permission'), function () {
                var update = dataservice.update('File', _file.Id);
                var relUpdate = update.attach('User', 'Access', vm.User());
                relUpdate.set('Type', vm.Permission());
                if (vm.Expire())
                    relUpdate.set('Expire', vm.Expire());
                $.when(update.execute()).done(function () {
                    popup.close();
                    load();
                });
            });
            popup.show();
        }

        function updatePerm(perm) {
            var update = dataservice.update('File', _file.Id);
            var relUpdate = update.updateRelation('User', 'Access', perm.Entity.Id);
            relUpdate.set('Type', perm.Data.Type());
            if (perm.Data.Expire())
                relUpdate.set('Expire', perm.Data.Expire());
            $.when(update.execute()).done(function () {
                perm.Original.Type(perm.Data.Type());
                if (perm.Data.Expire())
                    perm.Original.Expire(perm.Data.Expire());
            });
        }

        function revoke(perm) {
            var ask = presenter.ask(tp.get('confirm_entity_deletion'));
            ask.yes(function () {
                var update = dataservice.update('File', _file.Id);
                update.detach('User', 'Access', perm.Entity.Id);
                $.when(update.execute()).done(function () {
                    _file.RelationsData.User_Access.remove(perm);
                });
            });
            ask.show();
        }

        return {
            template: 'Files_ManageFile',
            bind: bind,
            Loaded: _loaded,
            File: _file,
            grant: addNewPerm,
            update: updatePerm,
            revoke: revoke
        };
    });
