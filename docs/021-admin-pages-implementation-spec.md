# Admin Pages Implementation Specification

## Overview
Complete the admin pages by replacing dummy data with real functionality and implementing missing components.

## Current Status

### ✅ Completed Pages
- **Users** - Full functionality with real data
- **User Details** - Complete user management 
- **Invites** - Fully functional invite system

### ⚠️ Pages Needing Real Data
- **Dashboard** - All dummy statistics and activity
- **Settings** - No persistence, mock toggles

### 🔨 Missing Pages
- Role creation/editing forms
- Role details view
- Role user assignments

## Implementation Plan

### 1. Dashboard Real Data (High Priority)
**File:** `Components/Admin/Dashboard.razor`

**Replace dummy data with:**
- Real user/role statistics from database
- Actual system health monitoring
- Live activity feed from audit logs
- Performance metrics collection

**New Service:** `IDashboardService` with methods:
- `GetStatisticsAsync()` - Real user counts, active sessions
- `GetSystemHealthAsync()` - Database/email/memory status
- `GetRecentActivityAsync()` - User activity from logs

### 2. Settings Persistence (High Priority)  
**File:** `Components/Admin/Settings.razor`

**Add functionality:**
- Database storage for all settings
- Form validation and save/reset
- JavaScript interactivity for toggles
- Email/backup system integration

**New Service:** `ISettingsService` with methods:
- `GetSettingsAsync()` - Load current settings
- `SaveSettingsAsync()` - Persist changes
- `TestEmailSettingsAsync()` - Validate SMTP

### 3. Missing Role Pages (Medium Priority)
**New Files:**
- `Components/Admin/RoleForm.razor` - Create/edit roles
- `Components/Admin/RoleDetails.razor` - View role details
- `Components/Admin/RoleUserAssignments.razor` - Manage assignments

**Enhanced Service:** Extend `AdminRoleService` with:
- `CreateRoleAsync()` - Create new roles
- `UpdateRoleAsync()` - Modify existing roles
- `GetRoleUsersAsync()` - Get users in role

### 4. User Experience Improvements (Medium Priority)
**Add throughout admin pages:**
- Toast notifications for user feedback
- Loading states and error handling
- Form validation with clear messages
- Confirmation dialogs for destructive actions

**New Components:**
- `Components/Shared/ToastNotification.razor`
- `Components/Shared/LoadingSpinner.razor`
- `Components/Shared/ConfirmDialog.razor`

## Database Changes Required

```sql
-- Settings storage
CREATE TABLE ApplicationSettings (
    Key NVARCHAR(255) PRIMARY KEY,
    Value NVARCHAR(MAX),
    IsEncrypted BIT DEFAULT 0
);

-- Activity logging  
CREATE TABLE SystemLogs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450),
    Action NVARCHAR(255),
    Details NVARCHAR(MAX),
    IpAddress NVARCHAR(45),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

## Implementation Steps

1. **Database Migration** - Add new tables
2. **Create Services** - Dashboard and Settings services
3. **Update Components** - Replace dummy data with service calls
4. **Add Missing Pages** - Role management components
5. **Enhance UX** - Add notifications and loading states
6. **Test Integration** - Verify all functionality works

## Success Criteria

- [ ] Dashboard shows real statistics and system health
- [ ] Settings can be saved and persist between sessions  
- [ ] All role management functionality is complete
- [ ] Users receive clear feedback for all actions
- [ ] All dummy data replaced with live data
- [ ] Mobile responsive design maintained