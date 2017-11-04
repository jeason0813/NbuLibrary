define('navigation',
    ['jquery', 'ko'],
    function ($, ko) {

        //todo: util candidate
        function endsWith(str, suffix) {
            return str.indexOf(suffix, str.length - suffix.length) !== -1;
        }

        var items = ko.observableArray([]);

        var vm = {
            items: items,
            Selected: ko.observable()
        };

        vm.onItemSelected = function(item){
            vm.Selected(item);
        };

        ko.applyBindings(vm, $('nav')[0]);

        $('nav').on('click', 'li', function (e) {
            vm.onItemSelected(ko.dataFor(this));
        });

        vm.endsWith = endsWith;

        function sortFunc(l, r) {
            if (l.Order == r.Order)
                return l.Label < r.Label ? -1 : 1;
            else
                return l.Order < r.Order ? -1 : 1;
        }

        function addItem(label, url, order, icon) {
            icon = icon ? icon : '';
            if (order == undefined || order == null)
                order = 10;
            var item = { Label: label, Url: url, Order : order, Icon : icon, hasSubmenu : ko.observable(false), items : ko.observableArray([]) };
            items.push(item);
            if (endsWith(window.location.href, url))
                vm.onItemSelected(item);

            items.sort(sortFunc);

            item.add = function (lbl, u, o, ic) {
                if (o === undefined || o === null)
                    o = 10;
                ic = ic ? ic : '';
                var subitem = { Label: lbl, Url: u, Order: o, Icon: ic };
                item.items.push(subitem);
                item.items.sort(sortFunc);
                item.hasSubmenu(true);
                //todo
            };            

            return item;
        };

        return {
            add: addItem
        };
    });