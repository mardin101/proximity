# Code Implementation Agent - Example Session

This document demonstrates a complete code implementation planning workflow, showing how the agent creates a comprehensive implementation plan for a well-defined sub-issue.

## Input: Sub-Issue from Decomposition Agent

The following sub-issue was created by the decomposition agent:

```markdown
# Sub-Issue #105: Implement Notification Backend Service

**Parent Issue**: #42 - Real-time Notifications System
**Priority**: 1 (Critical Path)
**Estimated Complexity**: Medium
**Type**: Backend

## Description

Create the core notification backend service that handles receiving, storing, and managing notifications for users. This service will be the foundation for the real-time notifications system and must be implemented before any frontend or real-time components.

## Scope

This sub-issue includes:
- Notification service class with business logic
- Database schema for storing notifications
- Database migrations
- Basic CRUD operations for notifications
- Event emitter for notification lifecycle events

This sub-issue excludes:
- API endpoints (separate sub-issue #106)
- Real-time WebSocket implementation (sub-issue #108)
- Frontend integration (sub-issue #110)

## Acceptance Criteria

1. [ ] NotificationService class implemented with create, findByUser, markAsRead, and delete methods
2. [ ] Database schema includes Notification model with userId, type, message, read status, and timestamps
3. [ ] Database migration created and can be run successfully
4. [ ] Service emits events for notification lifecycle (created, read, deleted)
5. [ ] Error handling for invalid user IDs and database failures
6. [ ] Service uses existing logging patterns for operation tracking

## Technical Details

- **Key files/components**: 
  - `src/services/notification.service.ts` (to create)
  - `prisma/schema.prisma` (to modify)
  - `prisma/migrations/` (new migration)
  
- **Dependencies**: 
  - Prisma (already in project)
  - eventemitter3 (already in project)
  
- **Testing approach**: 
  - Unit tests for service methods
  - Mock Prisma client for testing
  - Test event emissions

## Dependencies & Blockers

**Depends on**:
- [ ] Sub-Issue #103: Database setup (completed)
- [ ] Sub-Issue #104: Authentication middleware (completed)

**Blocks**:
- [ ] Sub-Issue #106: Notification API endpoints
- [ ] Sub-Issue #108: Real-time WebSocket notifications

## Suggested Labels

`sub-issue`, `backend`, `medium-complexity`, `notifications`

## Implementation Notes

- Follow the pattern established in `src/services/auth.service.ts` for service structure
- Use the EventEmitter pattern for lifecycle events
- Ensure all database operations are wrapped in try-catch blocks
- Log all significant operations using the existing logger utility
```

---

## Example Implementation Planning Session

**Agent**: Hello! I'm the Code Implementation Agent 🔨. I'll help you create a comprehensive implementation plan for your sub-issue that includes:
- Context from architecture docs and existing codebase
- Step-by-step implementation guidance with code examples
- Quality considerations (security, performance, accessibility)
- Clear next steps for testing and review

To get started, please provide:
- **Sub-issue URL or content** (including acceptance criteria)
- **Parent issue reference** (for broader context)
- **Any architecture/API docs** (if available)

I'll analyze the codebase, identify patterns, and create a detailed implementation plan that follows your project's conventions.

**User**: Here's sub-issue #105 for implementing the notification backend service. It's part of parent issue #42 - Real-time Notifications System. I don't have specific architecture docs, but the project follows standard Node.js/TypeScript patterns with Prisma for database access.

**Agent**: Thank you! Let me analyze the sub-issue and codebase to create a comprehensive implementation plan.

*[Agent analyzes codebase, scans for patterns, and gathers context]*

I've analyzed the sub-issue and gathered context. Here's my understanding:

**Sub-Issue**: #105 - Implement Notification Backend Service
**Parent Feature**: #42 - Real-time Notifications System
**Core Requirement**: Create a notification service with CRUD operations and event emission

**Architecture Reference**: Not found - will use codebase patterns
**API Specification**: Not found - will infer from requirements

**Key Components Identified**:
- NotificationService class (following `src/services/auth.service.ts` pattern)
- Prisma Notification model (following existing schema patterns)
- Database migration (using existing Prisma migrations)
- Event emitter integration (using eventemitter3 as in auth service)

**Existing Patterns Found**:
- Service pattern: `src/services/auth.service.ts`, `src/services/user.service.ts`
- Error handling: `src/utils/errors.ts` (ApiError class)
- Logging: `src/utils/logger.ts`
- Database: Prisma with existing User model in `prisma/schema.prisma`

Is this understanding correct? Please confirm or provide corrections.

**User**: Yes, that's correct! Please proceed with the implementation plan.

**Agent**: Excellent! I'll create a detailed implementation plan following your project's conventions.

---

# Implementation Plan: Implement Notification Backend Service

## 📋 Context Summary
- **Parent Feature**: #42 - Real-time Notifications System
- **Sub-Issue**: #105 - Implement Notification Backend Service
- **Architecture Reference**: Using codebase patterns (service pattern, Prisma, EventEmitter)
- **API Specification**: Inferring from requirements (API endpoints in separate sub-issue #106)
- **Dependencies**: Sub-issues #103 (Database setup) and #104 (Auth middleware) completed

## 🎯 Acceptance Criteria Mapping

**AC1**: NotificationService class implemented with create, findByUser, markAsRead, and delete methods
- **Implementation**: Create NotificationService class in `src/services/notification.service.ts`
- **Pattern**: Follow EventEmitter pattern from `src/services/auth.service.ts`
- **Files**: `src/services/notification.service.ts` (new)
- **Dependencies**: eventemitter3 (already in package.json)

**AC2**: Database schema includes Notification model with userId, type, message, read status, and timestamps
- **Implementation**: Add Notification model to Prisma schema
- **Pattern**: Follow existing User model structure in `prisma/schema.prisma`
- **Files**: `prisma/schema.prisma` (modify)
- **Dependencies**: Prisma (already configured)

**AC3**: Database migration created and can be run successfully
- **Implementation**: Create migration using `npx prisma migrate dev`
- **Pattern**: Follow existing migration pattern in `prisma/migrations/`
- **Files**: New migration in `prisma/migrations/`
- **Dependencies**: None (migration tool included with Prisma)

**AC4**: Service emits events for notification lifecycle (created, read, deleted)
- **Implementation**: Emit events using EventEmitter after each operation
- **Pattern**: Match event emission pattern from `src/services/auth.service.ts`
- **Files**: `src/services/notification.service.ts` (included in service)
- **Dependencies**: eventemitter3 (already available)

**AC5**: Error handling for invalid user IDs and database failures
- **Implementation**: Use try-catch blocks and ApiError class
- **Pattern**: Follow error handling in `src/services/auth.service.ts` using `src/utils/errors.ts`
- **Files**: `src/services/notification.service.ts` (error handling included)
- **Dependencies**: ApiError class from `src/utils/errors.ts`

**AC6**: Service uses existing logging patterns for operation tracking
- **Implementation**: Import and use logger from `src/utils/logger.ts`
- **Pattern**: Follow logging pattern from `src/services/user.service.ts`
- **Files**: `src/services/notification.service.ts` (logging included)
- **Dependencies**: Logger from `src/utils/logger.ts`

## 📁 Files to Create/Modify

### New Files
```
src/services/notification.service.ts       # Core notification service with business logic
src/services/__tests__/notification.service.test.ts  # Unit tests (to be created by Test Generation Agent)
```

### Modified Files
```
prisma/schema.prisma                       # Add Notification model and relation to User
prisma/migrations/YYYYMMDDHHMMSS_add_notifications/  # New migration directory
```

## 🔨 Implementation Steps

### Step 1: Update Database Schema

Add the Notification model to your Prisma schema:

```typescript
// File: prisma/schema.prisma

// Add this enum at the top level
enum NotificationType {
  INFO
  WARNING
  ERROR
  SUCCESS
}

// Add this model after the existing User model
model Notification {
  id        String           @id @default(cuid())
  userId    String
  type      NotificationType
  message   String
  read      Boolean          @default(false)
  createdAt DateTime         @default(now())
  updatedAt DateTime         @updatedAt
  
  // Relation to User
  user User @relation(fields: [userId], references: [id], onDelete: Cascade)
  
  // Indexes for performance
  @@index([userId])
  @@index([userId, createdAt])
  @@index([userId, read])
}

// Add this to the existing User model
model User {
  // ... existing fields ...
  
  // Add this relation
  notifications Notification[]
}
```

**Rationale**:
- `cuid()` for IDs matches existing User model pattern
- `NotificationType` enum provides type safety
- Indexes on userId and common query patterns for performance
- `onDelete: Cascade` ensures notifications are deleted when user is deleted
- `updatedAt` allows tracking when notification was marked as read

**Commands to run**:
```bash
# Create and apply migration
npx prisma migrate dev --name add_notifications

# Generate Prisma client with new types
npx prisma generate
```

### Step 2: Create Notification Service

Create the notification service with business logic:

```typescript
// File: src/services/notification.service.ts

import { EventEmitter } from 'eventemitter3';
import { Prisma, NotificationType } from '@prisma/client';
import { prisma } from '../config/database';
import { ApiError } from '../utils/errors';
import { logger } from '../utils/logger';

/**
 * Notification service for managing user notifications
 * Follows the service pattern established in auth.service.ts
 */
export class NotificationService extends EventEmitter {
  /**
   * Create a new notification for a user
   * @param userId - The user ID to create notification for
   * @param type - The type of notification (INFO, WARNING, ERROR, SUCCESS)
   * @param message - The notification message
   * @returns The created notification
   * @throws ApiError if userId is invalid or database operation fails
   */
  async createNotification(
    userId: string,
    type: NotificationType,
    message: string
  ) {
    // Validate input
    if (!userId || userId.trim() === '') {
      throw new ApiError(400, 'User ID is required');
    }

    if (!message || message.trim() === '') {
      throw new ApiError(400, 'Notification message is required');
    }

    try {
      // Verify user exists
      const userExists = await prisma.user.findUnique({
        where: { id: userId },
        select: { id: true }
      });

      if (!userExists) {
        throw new ApiError(404, `User with ID ${userId} not found`);
      }

      // Create notification
      const notification = await prisma.notification.create({
        data: {
          userId,
          type,
          message
        },
        include: {
          user: {
            select: {
              id: true,
              email: true,
              name: true
            }
          }
        }
      });

      // Log operation
      logger.info('Notification created', {
        notificationId: notification.id,
        userId: notification.userId,
        type: notification.type
      });

      // Emit event for notification created
      this.emit('notification:created', notification);

      return notification;
    } catch (error) {
      if (error instanceof ApiError) {
        throw error;
      }

      // Log database error
      logger.error('Failed to create notification', {
        error: error.message,
        userId,
        type
      });

      throw new ApiError(500, 'Failed to create notification');
    }
  }

  /**
   * Find all notifications for a user
   * @param userId - The user ID to find notifications for
   * @param options - Pagination and filtering options
   * @returns Array of notifications with pagination metadata
   * @throws ApiError if userId is invalid or database operation fails
   */
  async findByUser(
    userId: string,
    options: {
      limit?: number;
      offset?: number;
      unreadOnly?: boolean;
    } = {}
  ) {
    const { limit = 50, offset = 0, unreadOnly = false } = options;

    // Validate input
    if (!userId || userId.trim() === '') {
      throw new ApiError(400, 'User ID is required');
    }

    try {
      // Build where clause
      const where: Prisma.NotificationWhereInput = {
        userId
      };

      if (unreadOnly) {
        where.read = false;
      }

      // Fetch notifications with pagination
      const [notifications, total] = await Promise.all([
        prisma.notification.findMany({
          where,
          orderBy: {
            createdAt: 'desc'
          },
          take: limit,
          skip: offset
        }),
        prisma.notification.count({ where })
      ]);

      logger.info('Notifications retrieved', {
        userId,
        count: notifications.length,
        total,
        unreadOnly
      });

      return {
        notifications,
        pagination: {
          limit,
          offset,
          total
        }
      };
    } catch (error) {
      logger.error('Failed to retrieve notifications', {
        error: error.message,
        userId
      });

      throw new ApiError(500, 'Failed to retrieve notifications');
    }
  }

  /**
   * Mark a notification as read
   * @param notificationId - The notification ID to mark as read
   * @param userId - The user ID (for authorization)
   * @returns The updated notification
   * @throws ApiError if notification not found or unauthorized
   */
  async markAsRead(notificationId: string, userId: string) {
    // Validate input
    if (!notificationId || notificationId.trim() === '') {
      throw new ApiError(400, 'Notification ID is required');
    }

    if (!userId || userId.trim() === '') {
      throw new ApiError(400, 'User ID is required');
    }

    try {
      // Find notification and verify ownership
      const notification = await prisma.notification.findUnique({
        where: { id: notificationId }
      });

      if (!notification) {
        throw new ApiError(404, 'Notification not found');
      }

      if (notification.userId !== userId) {
        throw new ApiError(403, 'Unauthorized to modify this notification');
      }

      // Update notification
      const updatedNotification = await prisma.notification.update({
        where: { id: notificationId },
        data: {
          read: true
        }
      });

      logger.info('Notification marked as read', {
        notificationId,
        userId
      });

      // Emit event
      this.emit('notification:read', updatedNotification);

      return updatedNotification;
    } catch (error) {
      if (error instanceof ApiError) {
        throw error;
      }

      logger.error('Failed to mark notification as read', {
        error: error.message,
        notificationId,
        userId
      });

      throw new ApiError(500, 'Failed to update notification');
    }
  }

  /**
   * Delete a notification
   * @param notificationId - The notification ID to delete
   * @param userId - The user ID (for authorization)
   * @returns Success message
   * @throws ApiError if notification not found or unauthorized
   */
  async deleteNotification(notificationId: string, userId: string) {
    // Validate input
    if (!notificationId || notificationId.trim() === '') {
      throw new ApiError(400, 'Notification ID is required');
    }

    if (!userId || userId.trim() === '') {
      throw new ApiError(400, 'User ID is required');
    }

    try {
      // Find notification and verify ownership
      const notification = await prisma.notification.findUnique({
        where: { id: notificationId }
      });

      if (!notification) {
        throw new ApiError(404, 'Notification not found');
      }

      if (notification.userId !== userId) {
        throw new ApiError(403, 'Unauthorized to delete this notification');
      }

      // Delete notification
      await prisma.notification.delete({
        where: { id: notificationId }
      });

      logger.info('Notification deleted', {
        notificationId,
        userId
      });

      // Emit event
      this.emit('notification:deleted', { id: notificationId, userId });

      return { success: true, message: 'Notification deleted' };
    } catch (error) {
      if (error instanceof ApiError) {
        throw error;
      }

      logger.error('Failed to delete notification', {
        error: error.message,
        notificationId,
        userId
      });

      throw new ApiError(500, 'Failed to delete notification');
    }
  }

  /**
   * Get count of unread notifications for a user
   * @param userId - The user ID
   * @returns Count of unread notifications
   */
  async getUnreadCount(userId: string): Promise<number> {
    if (!userId || userId.trim() === '') {
      throw new ApiError(400, 'User ID is required');
    }

    try {
      const count = await prisma.notification.count({
        where: {
          userId,
          read: false
        }
      });

      return count;
    } catch (error) {
      logger.error('Failed to get unread count', {
        error: error.message,
        userId
      });

      throw new ApiError(500, 'Failed to get unread count');
    }
  }
}

// Export singleton instance
export const notificationService = new NotificationService();
```

**Key Design Decisions**:
- **EventEmitter pattern**: Matches `auth.service.ts` pattern for extensibility
- **Input validation**: Validates all inputs before database operations
- **Error handling**: Uses ApiError class consistently
- **Logging**: Logs all operations with context
- **Authorization**: Verifies user ownership before update/delete operations
- **Pagination**: Supports limit/offset for listing notifications
- **Singleton export**: Provides ready-to-use instance

### Step 3: Verify Integration

The service is now ready to be used. In the next sub-issue (#106), you'll create API endpoints that use this service:

```typescript
// Example usage (will be in sub-issue #106)
import { notificationService } from './services/notification.service';
import { NotificationType } from '@prisma/client';

// Create notification
const notification = await notificationService.createNotification(
  'user_123',
  NotificationType.INFO,
  'Welcome to the platform!'
);

// Listen to events
notificationService.on('notification:created', (notification) => {
  console.log('New notification:', notification.id);
  // Could trigger WebSocket event here
});

// Find user notifications
const result = await notificationService.findByUser('user_123', {
  limit: 10,
  offset: 0,
  unreadOnly: true
});
```

## 🔍 Code Patterns to Follow

**Error Handling**: 
```typescript
// From src/utils/errors.ts
export class ApiError extends Error {
  constructor(public statusCode: number, message: string) {
    super(message);
  }
}

// Usage in service
if (!userId) {
  throw new ApiError(400, 'User ID is required');
}
```

**Logging**: 
```typescript
// From src/utils/logger.ts
import { logger } from '../utils/logger';

// Usage in service
logger.info('Operation completed', { contextData });
logger.error('Operation failed', { error: error.message });
```

**Database Operations**:
```typescript
// Prisma pattern with error handling
try {
  const result = await prisma.model.operation({ /* options */ });
  return result;
} catch (error) {
  logger.error('Database operation failed', { error: error.message });
  throw new ApiError(500, 'Operation failed');
}
```

**Event Emission**:
```typescript
// EventEmitter pattern
this.emit('event:name', eventData);

// Usage by consumers
service.on('event:name', (data) => {
  // Handle event
});
```

## ⚠️ Edge Cases to Handle

1. **User doesn't exist**: Validated before creating notification (throws 404)
2. **Empty/null user ID**: Validated at method entry (throws 400)
3. **Empty/null message**: Validated at method entry (throws 400)
4. **Database connection failure**: Caught and logged, throws 500
5. **Concurrent read operations**: Prisma handles transaction isolation
6. **Notification not found**: Returns 404 for markAsRead and delete
7. **Unauthorized access**: Verifies userId ownership before update/delete (throws 403)
8. **Very long messages**: Could add max length validation (currently unlimited)

## 🔐 Security Considerations

- ✅ **Input validation**: All inputs validated before database operations
  - **Implementation**: Checks for empty/null values, validates userId exists
  - **Validation**: Unit tests for invalid inputs

- ✅ **Authorization**: Users can only access their own notifications
  - **Implementation**: userId verification in markAsRead and delete methods
  - **Validation**: Integration tests for cross-user access attempts

- ✅ **SQL injection prevention**: Using Prisma ORM with parameterized queries
  - **Implementation**: All queries through Prisma (no raw SQL)
  - **Validation**: Prisma handles this automatically

- ✅ **XSS prevention**: Notification messages should be sanitized by API layer
  - **Implementation**: To be added in API endpoints sub-issue #106
  - **Validation**: Integration tests in sub-issue #106

## 🚀 Performance Considerations

- **Database indexes**: Added indexes on userId, (userId, createdAt), and (userId, read)
  - **Approach**: Optimizes common queries (list by user, list unread)
  - **Reference**: Standard Prisma indexing pattern

- **Pagination**: Implemented limit/offset pagination in findByUser
  - **Approach**: Prevents loading all notifications at once
  - **Reference**: Similar pattern can be added to other services

- **Selective field loading**: Uses `select` to load only needed user fields
  - **Approach**: Reduces data transfer in createNotification
  - **Reference**: Prisma performance best practice

- **Cascade delete**: Database handles notification cleanup when user deleted
  - **Approach**: onDelete: Cascade in schema
  - **Reference**: Ensures no orphaned notifications

## 🧪 Testing Suggestions

**Unit Tests** (to be created by Test Generation Agent):
- `createNotification()` with valid input
- `createNotification()` with invalid userId (should throw 400)
- `createNotification()` with non-existent user (should throw 404)
- `createNotification()` emits 'notification:created' event
- `findByUser()` returns paginated results
- `findByUser()` filters unread only
- `markAsRead()` updates notification
- `markAsRead()` throws 403 for wrong user
- `deleteNotification()` removes notification
- `deleteNotification()` throws 403 for wrong user
- `getUnreadCount()` returns correct count
- Database error handling in all methods

**Integration Tests** (to be created in sub-issue #106):
- API endpoints using this service
- Database transactions
- Event emissions trigger downstream actions

**Edge Case Tests**:
- Empty string inputs
- Null values
- Very long messages
- Concurrent operations
- Database connection failures

## ✅ Pre-Implementation Checklist

- [x] Architecture document reviewed (using codebase patterns)
- [x] API specification reviewed (API in separate sub-issue)
- [x] Existing codebase patterns identified (auth.service.ts, user.service.ts)
- [x] Dependencies verified (Prisma and eventemitter3 already available)
- [x] Database migration plan ready (Prisma migrate dev)

## ✅ Implementation Checklist

- [ ] Database schema updated in `prisma/schema.prisma`
- [ ] Database migration created and run successfully
- [ ] NotificationService class implemented in `src/services/notification.service.ts`
- [ ] All CRUD methods implemented (create, findByUser, markAsRead, delete)
- [ ] Error handling added for all methods
- [ ] Logging added for all operations
- [ ] Input validation added for all methods
- [ ] Authorization checks added (ownership verification)
- [ ] Event emission added (created, read, deleted)
- [ ] Unread count method implemented (bonus)

## ✅ Post-Implementation Checklist

- [ ] All acceptance criteria met
- [ ] Code follows project conventions (matches auth.service.ts pattern)
- [ ] No linting errors (run `npm run lint`)
- [ ] Manual testing completed (create instance and test methods)
- [ ] Ready for Test Generation Agent
- [ ] Ready for API endpoints sub-issue #106

## 🔄 Next Steps

1. **Review this plan** - Adjust if needed based on additional context
2. **Implement following the steps above**:
   - Step 1: Update `prisma/schema.prisma`
   - Run `npx prisma migrate dev --name add_notifications`
   - Step 2: Create `src/services/notification.service.ts`
   - Verify no linting errors
3. **Manual testing**:
   ```typescript
   // In a test file or REPL
   import { notificationService } from './services/notification.service';
   import { NotificationType } from '@prisma/client';
   
   const notification = await notificationService.createNotification(
     'existing_user_id',
     NotificationType.INFO,
     'Test notification'
   );
   console.log(notification);
   ```
4. **Invoke Test Generation Agent** once implementation complete:
   - Command: `@test-gen generate tests for issue #105`
5. **Create PR** with:
   - Database schema changes
   - Migration files
   - NotificationService implementation
   - Note: Tests will be added after Test Generation Agent runs
6. **Request code review** from team

## 📚 Reference Documentation

- Architecture Doc: N/A (using codebase patterns)
- API Specification: N/A (separate sub-issue #106)
- Existing Patterns: 
  - `src/services/auth.service.ts` - Service structure and EventEmitter
  - `src/services/user.service.ts` - Database operations and logging
  - `src/utils/errors.ts` - Error handling
  - `src/utils/logger.ts` - Logging
  - `prisma/schema.prisma` - Schema patterns
- Project Conventions: Follow TypeScript and ESLint rules in project

---

## How This Implementation Plan Helps

This implementation plan:
1. ✅ Provides step-by-step guidance with actual code
2. ✅ References existing patterns in the codebase
3. ✅ Includes comprehensive error handling and logging
4. ✅ Maps all acceptance criteria to implementation
5. ✅ Identifies edge cases and security considerations
6. ✅ Provides clear next steps for testing and review
7. ✅ Ready for autonomous implementation or human developer guidance
8. ✅ Integrates with Test Generation Agent for comprehensive testing
