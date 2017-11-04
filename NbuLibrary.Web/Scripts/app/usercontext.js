define('usercontext',
    ['ko'],
    function (ko) {

        var UserTypes = {
            Customer: 0,
            Librarian: 1,
            Admin: 2
        };

        function currentUser() {
            return NbuLib.embed.user;
        }

        return {
            isAdmin: function () {
                return currentUser().UserType == UserTypes.Admin;
            },
            isCustomer: function () {
                return currentUser().UserType == UserTypes.Customer;
            },
            isLibrarian: function () {
                return currentUser().UserType == UserTypes.Librarian;
            },

            hasPermission: function (moduleId, perm) {
                var user = currentUser();
                if (user.UserType == UserTypes.Admin)
                    return true;
                else if (!user.UserGroup)
                    return false;
                else if (!user.UserGroup.ModulePermissions)
                    return false;

                //TODO: optimize - use hash table or 
                var mp = ko.utils.arrayFirst(user.UserGroup.ModulePermissions, function (x) { return x.ModuleID == moduleId; });
                if (!mp)
                    return false;
                else if (perm) //has the specified permission
                    return !!ko.utils.arrayFirst(mp.Granted, function (x) { return x.toLowerCase() == perm.toLowerCase(); });
                else
                    return true; //has some module permissions

            },

            currentUser: currentUser
        };
    });