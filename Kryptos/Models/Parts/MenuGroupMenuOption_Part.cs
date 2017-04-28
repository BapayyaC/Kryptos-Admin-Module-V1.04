using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace Kryptos.Models
{
    public partial class MenuGroupMenuOption
    {
        kryptoEntities1 ke = new kryptoEntities1();
        private MenuOption _menuOption;
        public MenuOption MenuOption
        {
            get
            {
                if (!MenuOptionId.HasValue) return null;
                if (_menuOption == null) _menuOption = ke.MenuOptions.SingleOrDefault(x => x.MenuOptionId.Equals(MenuOptionId.Value));
                return _menuOption;
            }
            set
            {
                _menuOption = value;
            }
        }

        private MenuGroup _menuGroup;
        public MenuGroup MenuGroup
        {
            get
            {
                if (MenuGroupId.HasValue)
                {
                    return _menuGroup ??
                           (_menuGroup = ke.MenuGroups.SingleOrDefault(x => x.MenuGroupId.Equals(MenuGroupId.Value)));
                }
                return null;
            }
            set
            {
                _menuGroup = value;
            }
        }

    }
}
