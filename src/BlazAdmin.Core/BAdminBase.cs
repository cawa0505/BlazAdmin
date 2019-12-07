﻿using BlazAdmin.Core;
using Blazui.Component;
using Blazui.Component.Container;
using Blazui.Component.EventArgs;
using Blazui.Component.Form;
using Blazui.Component.NavMenu;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazAdmin.Core
{
    public class BAdminBase : BComponentBase
    {
        [Inject]
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject]
        private RouteService routeService { get; set; }

        protected BForm form;
        [Inject]
        private MessageService MessageService { get; set; }

        [Inject]
        private MessageBox MessageBox { get; set; }

        protected string defaultMenuIndex;

        [Parameter]
        public LoginInfoModel DefaultUser { get; set; }

        [Parameter]
        public bool AutoInitilizePermissionMenus { get; set; } = true;
        protected string username;
        [Parameter]
        public RenderFragment LoginPage { get; set; }
        [Parameter]
        public RenderFragment CreatePage { get; set; }
        [Parameter]
        public RenderFragment ModifyPasswordPage { get; set; }
        [Parameter]
        public float NavigationWidth { get; set; } = 250;
        /// <summary>
        /// 导航菜单栏标题
        /// </summary>
        [Parameter]
        public string NavigationTitle { get; set; } = "BlazAdmin 后台模板";

        [Parameter]
        public List<MenuModel> Menus { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }
        [Parameter]
        public string DefaultRoute { get; set; }

        internal string ActiveTabName { get; set; }

        [Parameter]
        public RenderFragment Body { get; set; }

        protected IMenuItem CurrentMenu { get; set; }

        /// <summary>
        /// 页面刚刚加载完成时自动加载选项卡的动作是否完成
        /// </summary>
        private bool isLoadRendered = false;

        internal async Task ModifyPasswordAsync()
        {
            var modifyPasswordPage = ModifyPasswordPage;
            if (modifyPasswordPage == null)
            {
                modifyPasswordPage = builder =>
                {
                    builder.OpenComponent<BModifyPassword>(0);
                    builder.CloseComponent();
                };
            }
            var result = await DialogService.ShowDialogAsync<ModifyPasswordModel>(modifyPasswordPage, "修改密码", 500);
            if (result.Result == null)
            {
                return;
            }
            await form.SubmitAsync("/account/login?callback=" + NavigationManager.Uri);
        }

        internal async System.Threading.Tasks.Task LogoutAsync()
        {
            var result = await MessageBox.ConfirmAsync("是否确认注销登录？");
            if (result != MessageBoxResult.Ok)
            {
                return;
            }

            await form.SubmitAsync("/account/login?callback=" + NavigationManager.Uri);
        }
        /// <summary>
        /// 初始 Tab 集合
        /// </summary>
        [Parameter]
        public ObservableCollection<TabModel> Tabs { get; set; } = new ObservableCollection<TabModel>();

        protected void OnTabPanelChanging(BChangeEventArgs<ITab> args)
        {
            args.DisallowChange = true;
            NavigationManager.NavigateTo(args.NewValue.Name);
        }
        protected override async Task OnInitializedAsync()
        {
            var path = new Uri(NavigationManager.Uri).LocalPath;
            if (path == "/" && !string.IsNullOrWhiteSpace(DefaultRoute))
            {
                NavigationManager.NavigateTo(DefaultRoute);
                return;
            }

            FixMenuInfo(Menus);
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            username = user.Identity.Name;
            NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
            NavigationManager.LocationChanged += NavigationManager_LocationChanged;

            if (AutoInitilizePermissionMenus)
            {
                var permissionMenu = new MenuModel();
                permissionMenu.Label = "权限管理";
                permissionMenu.Name = "权限管理";
                permissionMenu.Icon = "el-icon-lock";
                permissionMenu.Children.Add(new MenuModel()
                {
                    Icon = "el-icon-user-solid",
                    Label = "用户列表",
                    Route = "/user/list",
                    Name = "userlist",
                    Title = "用户列表"
                });
                permissionMenu.Children.Add(new MenuModel()
                {
                    Icon = "el-icon-s-custom",
                    Label = "角色列表",
                    Route = "/user/roles",
                    Name = "rolelist",
                    Title = "角色列表"
                });
                permissionMenu.Children.Add(new MenuModel()
                {
                    Icon = "el-icon-s-grid",
                    Label = "功能列表",
                    Name = "featurelist",
                    Route = "/user/features",
                    Title = "功能列表"
                });
                Menus.Add(permissionMenu);
            }
            FindMenuName(Menus, path);
        }

        private void NavigationManager_LocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            var path = new Uri(e.Location).LocalPath;
            AddTab(path);
        }

        void FindMenuName(List<MenuModel> menus, string path)
        {
            foreach (var menu in menus)
            {
                if (menu.Route == path)
                {
                    defaultMenuIndex = menu.Name;
                    return;
                }
                FindMenuName(menu.Children, path);
            }
        }

        void FixMenuInfo(List<MenuModel> menus)
        {
            foreach (var menu in menus)
            {
                menu.Name = menu.Name ?? menu.Route;
                menu.Title = menu.Title ?? menu.Label;
                FixMenuInfo(menu.Children);
            }
        }

        internal void Refresh()
        {
            StateHasChanged();
        }
        protected override void OnAfterRender(bool firstRender)
        {
            if (!isLoadRendered)
            {
                isLoadRendered = true;
                var path = new Uri(NavigationManager.Uri).LocalPath;
                AddTab(path);
            }
        }

        private void AddTab(string path)
        {
            var type = routeService.GetComponent(path);
            if (type == null)
            {
                if (path != "/")
                {
                    MessageService.Show($"路由为 {path} 的页面未找到", MessageType.Warning);
                }
                return;
            }
            ActiveTabName = path;
            if (!Tabs.Any(x => x.Name == ActiveTabName))
            {
                if (CurrentMenu == null)
                {
                    return;
                }
                var model = (MenuModel)CurrentMenu.Model;
                Tabs.Add(new TabModel()
                {
                    Title = model.Title ?? model.Label,
                    Name = ActiveTabName,
                    Content = type
                });
            }

            StateHasChanged();
        }
    }
}
