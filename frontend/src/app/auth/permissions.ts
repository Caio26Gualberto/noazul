export const Permissions = {
  adminAccess: 'Admin.Access',

  itemsView: 'Items.View',
  itemsCreate: 'Items.Create',
  itemsEdit: 'Items.Edit',
  itemsDelete: 'Items.Delete',

  authorizationUsersView: 'Authorization.Users.View',
  authorizationUsersInvite: 'Authorization.Users.Invite',
  authorizationUsersDelete: 'Authorization.Users.Delete',
  authorizationUsersAssignRole: 'Authorization.Users.AssignRole',
  authorizationUsersManagePermissions: 'Authorization.Users.ManagePermissions',
  authorizationRolesView: 'Authorization.Roles.View',
  authorizationRolesCreate: 'Authorization.Roles.Create',
  authorizationRolesEdit: 'Authorization.Roles.Edit',
  authorizationRolesDelete: 'Authorization.Roles.Delete',
  authorizationRolesManagePermissions: 'Authorization.Roles.ManagePermissions',
  authorizationAuditView: 'Authorization.Audit.View',

  systemJobsView: 'System.Jobs.View',
  systemJobsManage: 'System.Jobs.Manage',
  systemAuditView: 'System.Audit.View',
} as const;

export type Permission = (typeof Permissions)[keyof typeof Permissions];
