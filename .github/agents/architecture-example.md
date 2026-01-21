# Architecture Agent - Example Session

This document demonstrates a complete architecture design workflow, showing how the agent analyzes a feature from the decomposition phase and creates a comprehensive technical design.

## Input: Sub-Issue from Decomposition

The following sub-issue was created by the decomposition agent:

```markdown
# Sub-Issue #2: Build Real-Time Notification System

## Parent Issue
Smart Error Documentation Assistant (#105)

## Description
Implement a real-time notification system to deliver documentation suggestions instantly when errors occur. This system will use WebSocket connections for live updates and integrate with the existing error handling framework.

## Acceptance Criteria
1. ✅ WebSocket server established for persistent connections
2. ✅ Client automatically reconnects if connection drops
3. ✅ Notifications delivered within 100ms of error occurrence
4. ✅ Support for multiple concurrent connections (target: 1000+)
5. ✅ Authentication integrated with existing JWT system

## Technical Details
- Must integrate with error handling framework
- Real-time delivery required (not polling)
- Should scale to handle peak load
- Needs authentication for security
- Mobile and web client support

## Dependencies
- Sub-Issue #1: Error Categorization System (must be complete)
- Existing: JWT authentication system
- Existing: Error handling framework

## Estimated Complexity
High (7/10) - Real-time infrastructure, scaling concerns, multiple clients
```

## Example Architecture Session

**Agent**: Hello! I'm the Architecture Agent. I'll help you design the technical architecture for this feature.

I see you have a sub-issue for building a Real-Time Notification System. Let me analyze the complexity to determine if full architecture design is needed.

---

### Complexity Assessment

🔍 **Analyzing complexity...**

**Feature**: Real-Time Notification System for Error Documentation
**Scope**: WebSocket infrastructure with authentication, scaling, and multi-client support

📊 **Complexity Assessment**:
- **Estimated LOC**: ~1200 LOC
- **Components affected**: 
  1. New WebSocket server
  2. Frontend notification receiver
  3. Integration with error handler
  4. Authentication middleware
- **New infrastructure**: Yes (WebSocket server, possibly message queue)
- **Integration points**: 
  1. Existing error handling framework
  2. Existing JWT authentication system
  3. Mobile and web clients
- **Database changes**: Moderate (connection state, message persistence)
- **API surface area**: 1 WebSocket connection, multiple message types
- **Performance requirements**: <100ms delivery, 1000+ concurrent connections

**Complexity Score**: 8/10 (**High**)

**Recommendation**: ✅ **Full architecture design needed**

This feature introduces new real-time infrastructure, has significant scaling requirements, and touches multiple system components. A comprehensive architecture will help ensure proper design decisions around scalability, reliability, and integration patterns.

---

**Agent**: Before I design the architecture, I need to understand a few things:

**Questions**:

1. **Performance Requirements**: You mentioned 1000+ concurrent connections. What's the expected peak load, and do we need to support more in the future?

2. **Existing System Context**: Can you describe your current backend technology stack and where the error handling framework lives?

3. **Technology Constraints**: Any required or prohibited technologies for the WebSocket implementation?

4. **Timeline**: Is this for MVP or full production scale?

**User**: 
1. Peak load is estimated at 2000 concurrent connections initially, with potential to grow to 10,000 within a year.
2. Backend is Node.js with Express. Error handling is a middleware at `src/middleware/errorHandler.js`.
3. Prefer Socket.io for WebSocket due to team familiarity. No prohibited technologies.
4. This is for production MVP, but should be designed to scale.

---

**Agent**: Perfect! I have enough context to design a comprehensive architecture. Here's the technical design:

---

## Architecture Design Document

### Executive Summary

The Real-Time Notification System will deliver error documentation suggestions to users instantly using WebSocket connections. The architecture uses Socket.io for WebSocket management, Redis for pub/sub messaging and connection state, and integrates seamlessly with the existing Express middleware pattern. The design supports horizontal scaling to handle 10,000+ concurrent connections and provides <100ms notification delivery.

---

### System Architecture

#### Component Diagram

```mermaid
graph TB
    subgraph "Client Layer"
        WEB[Web Client<br/>Socket.io Client]
        MOBILE[Mobile Client<br/>Socket.io Client]
    end
    
    subgraph "Load Balancing"
        LB[Load Balancer<br/>Nginx/ALB]
    end
    
    subgraph "Application Layer"
        WS1[WebSocket Server 1<br/>Socket.io + Express]
        WS2[WebSocket Server 2<br/>Socket.io + Express]
        WS3[WebSocket Server N<br/>Socket.io + Express]
    end
    
    subgraph "Messaging Layer"
        REDIS[(Redis Pub/Sub)]
    end
    
    subgraph "Existing Systems"
        ERROR[Error Handler<br/>Middleware]
        AUTH[Auth Service<br/>JWT Validation]
        DOC[Documentation<br/>Service]
    end
    
    subgraph "Data Layer"
        DB[(PostgreSQL<br/>Connection State)]
        CACHE[(Redis Cache<br/>Session Data)]
    end
    
    WEB --> LB
    MOBILE --> LB
    LB --> WS1
    LB --> WS2
    LB --> WS3
    
    WS1 --> REDIS
    WS2 --> REDIS
    WS3 --> REDIS
    
    WS1 --> AUTH
    WS2 --> AUTH
    WS3 --> AUTH
    
    ERROR --> REDIS
    ERROR --> DOC
    
    WS1 --> DB
    WS1 --> CACHE
    
    style WS1 fill:#e1f5ff
    style WS2 fill:#e1f5ff
    style WS3 fill:#e1f5ff
    style REDIS fill:#ffe1e1
```

#### Data Flow Sequence

```mermaid
sequenceDiagram
    participant Client
    participant LoadBalancer
    participant WSServer
    participant Redis
    participant Auth
    participant ErrorHandler
    participant DocService
    
    Note over Client,WSServer: Connection Establishment
    Client->>LoadBalancer: Connect (wss://)
    LoadBalancer->>WSServer: Route connection
    WSServer->>Client: Connection established
    Client->>WSServer: Auth message (JWT)
    WSServer->>Auth: Validate JWT
    Auth-->>WSServer: Valid token
    WSServer->>Redis: Register connection
    WSServer->>Client: Auth success
    
    Note over ErrorHandler,Client: Error Occurs & Notification Flow
    ErrorHandler->>DocService: Get suggestions for error
    DocService-->>ErrorHandler: Top 3 docs
    ErrorHandler->>Redis: Publish notification (user_id, docs)
    Redis->>WSServer: Deliver to subscribed servers
    WSServer->>Client: Send notification (WebSocket)
    Client->>Client: Display suggestions
```

---

### Component Descriptions

#### WebSocket Server

**Purpose**: Manages persistent WebSocket connections and routes notifications to connected clients.

**Technology**: Node.js, Express, Socket.io v4

**Responsibilities**:
- Accept and authenticate WebSocket connections
- Maintain connection state and heartbeat
- Subscribe to Redis pub/sub channels for user notifications
- Deliver notifications to appropriate clients
- Handle connection drops and reconnection
- Emit connection metrics

**Interfaces**:
- **Input**: WebSocket connections from clients, notifications from Redis pub/sub
- **Output**: Real-time notifications to clients, connection state to Redis

**Dependencies**:
- Express server (shares HTTP port or separate)
- Socket.io library
- Redis client for pub/sub
- JWT validation middleware

**Configuration**:
```javascript
{
  port: 3001,                    // WebSocket server port
  corsOrigins: ['https://app.example.com'],
  heartbeatInterval: 30000,      // 30 seconds
  maxConnections: 5000,          // Per server instance
  redisUrl: 'redis://localhost:6379',
  jwtSecret: process.env.JWT_SECRET
}
```

---

#### Redis Pub/Sub Layer

**Purpose**: Enables horizontal scaling by routing notifications across multiple WebSocket server instances.

**Technology**: Redis v6+ (pub/sub + cache)

**Responsibilities**:
- Publish notifications to subscriber channels
- Route notifications to correct WebSocket server instances
- Store active connection state (user → server mapping)
- Cache user session data for quick authentication

**Interfaces**:
- **Input**: Notification publications from error handler
- **Output**: Notification delivery to subscribed WebSocket servers

**Data Structures**:
```javascript
// Connection Registry (Hash)
connections:{user_id} = {
  server_id: 'ws-server-1',
  connected_at: 1642684800,
  last_heartbeat: 1642684830
}

// Pub/Sub Channels
channels:user:{user_id}           // User-specific notifications
channels:broadcast                 // System-wide notifications

// Session Cache (Hash with TTL)
session:{user_id} = {
  jwt_token: 'hashed_token',
  permissions: ['read:docs'],
  expires_at: 1642688400
}
```

---

#### Integration with Error Handler

**Purpose**: Trigger notifications when errors occur and documentation is matched.

**Technology**: Express middleware pattern (existing)

**Integration Type**: Publisher to Redis

**Changes Required**:
```javascript
// src/middleware/errorHandler.js
async function handleError(err, req, res, next) {
  // Existing error handling logic...
  
  // NEW: Get documentation suggestions
  const suggestions = await documentationService.getSuggestions(err);
  
  if (suggestions.length > 0 && req.user) {
    // NEW: Publish notification via Redis
    await notificationService.publish(req.user.id, {
      type: 'error_docs',
      error: {
        code: err.code,
        message: err.message
      },
      suggestions: suggestions
    });
  }
  
  // Continue with existing error response...
  res.status(err.status || 500).json({...});
}
```

---

### Data Architecture

#### Data Models

##### Entity: WebSocketConnection

```typescript
interface WebSocketConnection {
  id: string;                    // UUID, primary key
  user_id: string;               // Foreign key to User
  socket_id: string;             // Socket.io socket ID
  server_id: string;             // Which WS server instance
  client_type: 'web' | 'mobile'; // Client type
  user_agent: string;            // Client info
  ip_address: string;            // Client IP
  connected_at: Date;            // Connection timestamp
  last_heartbeat: Date;          // Last ping received
  disconnected_at: Date | null;  // Disconnection timestamp
  disconnect_reason: string | null; // Reason for disconnect
}
```

**Validation Rules**:
- `user_id`: Required, must exist in User table
- `socket_id`: Required, unique per connection
- `server_id`: Required, identifies which server instance

**Indexes**:
- Primary: `id`
- Secondary: `user_id`, `socket_id`
- Composite: `(user_id, connected_at)` for history queries

**Lifecycle**:
- Created on WebSocket connection
- Updated on each heartbeat
- Marked as disconnected (not deleted) for audit trail

---

##### Entity: Notification

```typescript
interface Notification {
  id: string;                    // UUID, primary key
  user_id: string;               // Recipient
  type: string;                  // Notification type
  payload: object;               // Notification data
  delivered: boolean;            // Delivery status
  read: boolean;                 // Read status
  created_at: Date;              // Creation timestamp
  delivered_at: Date | null;     // When delivered
  read_at: Date | null;          // When read
  expires_at: Date;              // Expiration (for cleanup)
}
```

**Validation Rules**:
- `type`: One of: 'error_docs', 'system', 'alert'
- `payload`: JSON, max 10KB
- `expires_at`: Default 7 days from creation

**Indexes**:
- Primary: `id`
- Secondary: `user_id`, `created_at`, `delivered`, `expires_at`
- Composite: `(user_id, delivered, created_at)` for undelivered queries

---

#### Database Schema

**Table: `websocket_connections`**

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | UUID | PRIMARY KEY | Unique identifier |
| `user_id` | UUID | NOT NULL, FOREIGN KEY | User ID |
| `socket_id` | VARCHAR(255) | NOT NULL | Socket.io ID |
| `server_id` | VARCHAR(100) | NOT NULL | Server instance ID |
| `client_type` | VARCHAR(20) | NOT NULL | 'web' or 'mobile' |
| `user_agent` | TEXT | NULL | Client user agent |
| `ip_address` | INET | NULL | Client IP |
| `connected_at` | TIMESTAMP | NOT NULL, DEFAULT NOW() | Connection time |
| `last_heartbeat` | TIMESTAMP | NOT NULL, DEFAULT NOW() | Last heartbeat |
| `disconnected_at` | TIMESTAMP | NULL | Disconnection time |
| `disconnect_reason` | VARCHAR(255) | NULL | Reason for disconnect |

**Indexes**:
```sql
CREATE INDEX idx_ws_user_id ON websocket_connections(user_id);
CREATE INDEX idx_ws_socket_id ON websocket_connections(socket_id);
CREATE INDEX idx_ws_connected_at ON websocket_connections(connected_at DESC);
CREATE INDEX idx_ws_active ON websocket_connections(user_id, connected_at) 
  WHERE disconnected_at IS NULL;
```

**Foreign Keys**:
```sql
ALTER TABLE websocket_connections
  ADD CONSTRAINT fk_ws_user
  FOREIGN KEY (user_id)
  REFERENCES users(id)
  ON DELETE CASCADE;
```

---

#### Database Migration Plan

**Phase 1: Schema Creation**
```sql
-- Migration: 001_create_websocket_connections.sql
CREATE TABLE websocket_connections (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  socket_id VARCHAR(255) NOT NULL,
  server_id VARCHAR(100) NOT NULL,
  client_type VARCHAR(20) NOT NULL CHECK (client_type IN ('web', 'mobile')),
  user_agent TEXT,
  ip_address INET,
  connected_at TIMESTAMP NOT NULL DEFAULT NOW(),
  last_heartbeat TIMESTAMP NOT NULL DEFAULT NOW(),
  disconnected_at TIMESTAMP,
  disconnect_reason VARCHAR(255)
);

CREATE INDEX idx_ws_user_id ON websocket_connections(user_id);
CREATE INDEX idx_ws_socket_id ON websocket_connections(socket_id);
CREATE INDEX idx_ws_active ON websocket_connections(user_id, connected_at) 
  WHERE disconnected_at IS NULL;
```

```sql
-- Migration: 002_create_notifications.sql
CREATE TABLE notifications (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  type VARCHAR(50) NOT NULL,
  payload JSONB NOT NULL,
  delivered BOOLEAN NOT NULL DEFAULT false,
  read BOOLEAN NOT NULL DEFAULT false,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  delivered_at TIMESTAMP,
  read_at TIMESTAMP,
  expires_at TIMESTAMP NOT NULL DEFAULT (NOW() + INTERVAL '7 days')
);

CREATE INDEX idx_notif_user_id ON notifications(user_id);
CREATE INDEX idx_notif_created ON notifications(created_at DESC);
CREATE INDEX idx_notif_undelivered ON notifications(user_id, delivered, created_at)
  WHERE delivered = false;
CREATE INDEX idx_notif_expires ON notifications(expires_at) WHERE delivered = true;
```

**Phase 2: Cleanup Job**
- Scheduled job to delete expired notifications (keeps DB size manageable)
- Archive old connection logs (move to cold storage after 30 days)

**Rollback Strategy**:
```sql
-- Rollback: Drop tables in reverse order
DROP TABLE IF EXISTS notifications CASCADE;
DROP TABLE IF EXISTS websocket_connections CASCADE;
```

---

### Technology Stack Decisions

#### Backend Framework
**Choice**: Node.js with Express (existing stack)
**Rationale**: 
- Team already familiar with Node.js/Express
- Excellent WebSocket support via Socket.io
- Non-blocking I/O ideal for concurrent connections
- Easy integration with existing middleware

#### WebSocket Library
**Choice**: Socket.io v4
**Rationale**:
- Team familiarity (specified in requirements)
- Built-in reconnection logic
- Fallback to long polling for compatibility
- Rooms/namespaces for easy broadcasting
- Active community and excellent documentation

**Alternatives Considered**: 
- Native WebSocket (ws library): Lower-level, more control but more complexity
- Primus: Abstraction over multiple transports, adds unnecessary layer

#### Message Queue/Pub-Sub
**Choice**: Redis Pub/Sub with Redis Cache
**Rationale**:
- Enables horizontal scaling across multiple WS server instances
- Low latency (<1ms) for real-time requirements
- Can also serve as cache for session data
- Simple pub/sub API
- Already in stack for other caching needs

**Alternatives Considered**:
- RabbitMQ: More features but overkill for this use case
- AWS SNS/SQS: Cloud-dependent, higher latency

#### Database
**Choice**: PostgreSQL (existing)
**Rationale**:
- Already in use for user data
- JSONB support for flexible notification payloads
- Excellent indexing for connection queries
- ACID guarantees for audit trail

---

### Architecture Decision Records (ADRs)

#### ADR-001: Use Redis Pub/Sub for Cross-Server Notification Routing

**Status**: Proposed

**Context**: 
WebSocket connections will be distributed across multiple server instances for scaling. When an error occurs, we need to route the notification to the correct server instance that holds the user's WebSocket connection.

**Decision**: 
Use Redis Pub/Sub as the message bus for routing notifications across WebSocket server instances. Each server subscribes to user-specific channels and system broadcast channels.

**Consequences**:

**Positive**:
- **Horizontal scaling**: Add more WS servers without changing architecture
- **Low latency**: Redis pub/sub delivers messages in <1ms
- **Simple implementation**: Redis client libraries handle reconnection
- **Dual purpose**: Redis also serves as cache for session data

**Negative**:
- **Single point of failure**: Redis down means no notifications (mitigated by Redis Sentinel)
- **Memory usage**: All servers receive all messages (mitigated by user-specific channels)
- **Message persistence**: Pub/sub doesn't persist messages if no subscribers

**Mitigations**:
- Deploy Redis in Sentinel mode for high availability
- Use user-specific channels (channel:user:{user_id}) to reduce broadcast overhead
- Persist notifications to PostgreSQL for audit and retry logic
- Implement fallback to polling if Redis is unavailable

---

#### ADR-002: Store Connection State in Both Redis and PostgreSQL

**Status**: Proposed

**Context**: 
We need to know which server instance holds a user's WebSocket connection for routing. This state needs to be fast to query (for routing) but also durable (for analytics and debugging).

**Decision**: 
Store active connection state in Redis (fast, ephemeral) and persist full connection history in PostgreSQL (durable, queryable). Redis is source of truth for active connections; PostgreSQL is for historical analysis.

**Consequences**:

**Positive**:
- **Fast routing**: Redis lookup is <1ms for determining which server has the connection
- **Historical data**: PostgreSQL provides full audit trail of connections
- **Separation of concerns**: Real-time vs. analytics workloads isolated

**Negative**:
- **Dual writes**: Must update both Redis and PostgreSQL on connection events
- **Potential inconsistency**: Redis and DB could drift out of sync
- **Increased complexity**: Two data stores to manage

**Mitigations**:
- Redis is primary for active state; PostgreSQL updated async (eventual consistency OK)
- Periodic sync job reconciles Redis and DB
- Connection state in PostgreSQL includes timestamps for auditing sync issues

---

#### ADR-003: Implement Sticky Sessions at Load Balancer Level

**Status**: Proposed

**Context**: 
Socket.io requires sticky sessions (same client always routes to same server instance) to maintain WebSocket connection state between HTTP upgrade and WebSocket frames.

**Decision**: 
Configure load balancer (Nginx or ALB) to use sticky sessions based on Socket.io session ID. This ensures WebSocket upgrade handshake and subsequent frames go to the same server.

**Consequences**:

**Positive**:
- **Connection stability**: Client consistently connected to same server
- **Socket.io compatibility**: Handles HTTP-to-WebSocket upgrade correctly
- **Load balancer managed**: No application-level routing logic needed

**Negative**:
- **Uneven distribution**: Some servers may have more connections than others
- **Server restart impact**: All connections on that server drop (but auto-reconnect)
- **Session affinity overhead**: Slightly more complex load balancer config

**Mitigations**:
- Monitor connection distribution across servers and rebalance if needed
- Implement graceful shutdown: stop accepting new connections, drain existing
- Short-lived sticky sessions: If connection drops, next connection can go to different server

---

### Integration Points

#### Integration with Error Handler Middleware

**Integration Type**: Publisher to Redis

**Direction**: Outbound (Error Handler → Redis → WebSocket Servers)

**Data Exchanged**:
- **Sent**: Notification objects (user_id, error info, documentation suggestions)

**Implementation**:
```javascript
// src/services/notificationService.js
class NotificationService {
  constructor(redisClient) {
    this.redis = redisClient;
  }
  
  async publish(userId, notification) {
    const channel = `user:${userId}`;
    const message = JSON.stringify({
      user_id: userId,
      type: notification.type,
      payload: notification.payload,
      timestamp: new Date().toISOString()
    });
    
    // Publish to Redis
    await this.redis.publish(channel, message);
    
    // Also persist to DB for audit
    await Notification.create({
      user_id: userId,
      type: notification.type,
      payload: notification.payload
    });
  }
}
```

**Error Handling**: 
- If Redis publish fails, log error and persist to DB
- Background job can retry delivery from DB later

**Impact on Error Handler**: 
- Minimal: Single async call to notification service
- No blocking: Published to Redis asynchronously
- Backwards compatible: Works even if notification system is down

---

#### Integration with JWT Authentication Service

**Integration Type**: Validation service

**Direction**: Inbound (WebSocket Server → Auth Service)

**Data Exchanged**:
- **Sent**: JWT token from client
- **Received**: User ID, permissions, token validity

**Implementation**:
```javascript
// WebSocket authentication middleware
io.use(async (socket, next) => {
  try {
    const token = socket.handshake.auth.token;
    const user = await authService.validateToken(token);
    
    if (!user) {
      return next(new Error('Authentication failed'));
    }
    
    socket.userId = user.id;
    socket.userPermissions = user.permissions;
    next();
  } catch (error) {
    next(new Error('Authentication failed'));
  }
});
```

**Frequency**: Once per WebSocket connection establishment

**Error Handling**: Reject connection if authentication fails

**Dependencies**: Existing JWT validation logic in `src/services/authService.js`

---

### Security Considerations

#### Authentication
- **Method**: JWT Bearer tokens (consistent with existing auth system)
- **Token in**: Sent in first message after connection or in connection query parameter
- **Validation**: Uses existing JWT validation service
- **Token expiry**: Clients must reconnect when token expires (auto-handled by Socket.io)

#### Authorization
- **Connection-level**: User must be authenticated to connect
- **Channel-level**: Users can only receive notifications for their own user_id
- **Message-level**: Server validates user_id in notification matches socket.userId

#### Data Protection
- **In Transit**: WSS (WebSocket Secure) with TLS 1.2+
- **At Rest**: Notification payloads stored in PostgreSQL (encrypted at rest)
- **PII Handling**: User IDs and error messages only; no sensitive data in notifications

#### Input Validation
- **WebSocket messages**: Validate message format and types
- **Connection params**: Sanitize user agent and IP address
- **Notification payload**: Validate against schema before publishing

#### Rate Limiting
- **Connection rate**: Max 10 connections per user
- **Message rate**: Max 100 messages per minute per user (prevents DoS)
- **Notification rate**: Max 50 notifications per minute per user

#### Vulnerability Mitigation
- **XSS**: Notification payloads sanitized before display in client
- **CSRF**: N/A (WebSocket not vulnerable to CSRF)
- **DoS**: Rate limiting + max connections per user + connection timeout
- **Injection**: Parameterized queries for all DB operations

---

### Performance Considerations

#### Performance Requirements

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Notification Delivery | < 100ms (p95) | Time from error to client receipt |
| Connection Establishment | < 500ms | Time from handshake to auth success |
| Heartbeat Latency | < 50ms (p95) | Ping-pong round trip time |
| Concurrent Connections | 2000 (MVP), 10,000 (future) | Active socket count per cluster |

#### Optimization Strategies

**Connection Pooling**:
- Redis connection pool (5-10 connections per WS server)
- PostgreSQL connection pool (10-20 connections per WS server)
- Reuse connections across socket operations

**Message Batching**:
- If multiple notifications for same user, batch into single WebSocket frame
- Reduces network overhead for high-frequency scenarios

**Caching**:
- Cache JWT validation results in Redis (5-minute TTL)
- Cache user permissions in memory (refreshed on token validation)
- Reduces load on auth service

**Async Operations**:
- Persist notifications to DB asynchronously (don't block WebSocket send)
- Update connection heartbeat in background
- Clean up old connections in scheduled job

**Resource Limits**:
- Max 5,000 connections per WebSocket server instance
- Max 10KB payload size per notification
- Disconnect idle connections after 5 minutes of no activity

#### Load Testing Plan

**Scenario 1: Normal Load**
- 500 concurrent connections
- 10 notifications per second
- Expected: <50ms delivery latency, <2% CPU usage per server

**Scenario 2: Peak Load**
- 2,000 concurrent connections
- 50 notifications per second
- Expected: <100ms delivery latency, <10% CPU usage per server

**Scenario 3: Stress Test**
- 5,000 concurrent connections per server
- 100 notifications per second
- Expected: Identify breaking point, test auto-scaling

**Tools**: k6 for WebSocket load testing, Prometheus + Grafana for monitoring

---

### Scalability Plan

#### Current Scale (MVP)
- **Users**: 1,000 active users
- **Connections**: 2,000 concurrent connections (some users multiple devices)
- **Notifications**: ~500 per hour (based on error frequency)
- **Infrastructure**: 2 WebSocket servers + 1 Redis instance

#### Growth Projection

**6 Months**:
- **Users**: 5,000 active users
- **Connections**: 8,000 concurrent connections
- **Notifications**: ~2,000 per hour
- **Infrastructure**: 4 WebSocket servers + Redis Sentinel (3 nodes)

**12 Months**:
- **Users**: 15,000 active users
- **Connections**: 25,000 concurrent connections
- **Notifications**: ~8,000 per hour
- **Infrastructure**: 10 WebSocket servers + Redis Sentinel + PostgreSQL read replicas

#### Scaling Strategy

**Horizontal Scaling** (Preferred):
- Add more WebSocket server instances (5,000 connections each)
- Load balancer distributes new connections across servers
- Redis Pub/Sub handles routing across servers
- Auto-scaling based on connection count (trigger at 80% capacity)

**Database Scaling**:
- PostgreSQL read replicas for connection history queries
- Partition notifications table by month (for cleanup and archival)
- Move old connection logs to cold storage (S3) after 30 days

**Redis Scaling**:
- Redis Sentinel for high availability (master-slave replication)
- If pub/sub throughput becomes bottleneck, consider Redis Cluster
- Separate Redis instances for cache vs. pub/sub (isolate workloads)

**Bottlenecks & Mitigation**:
- **Redis pub/sub throughput**: Use user-specific channels, upgrade Redis instance size
- **WebSocket server memory**: Each connection ~10KB, 5000 connections = 50MB per server (not a bottleneck)
- **Database writes**: Async writes + batch insertions for connection logs

---

### Error Handling & Resilience

#### Failure Scenarios

**Scenario 1: WebSocket Server Crash**
- **Trigger**: Server process dies unexpectedly
- **Impact**: All connections on that server drop (~2,000 users affected)
- **Detection**: Health check failures, connection metrics drop
- **Recovery**: 
  - Clients auto-reconnect to different server (Socket.io client feature)
  - Load balancer routes to healthy servers
  - Undelivered notifications stored in DB, redelivered on reconnect
- **Manual Intervention**: Restart failed server, investigate crash logs

---

**Scenario 2: Redis Pub/Sub Failure**
- **Trigger**: Redis crashes or network partition
- **Impact**: Notifications not routed across servers; only users on the server that detected the error receive notifications
- **Detection**: Redis connection errors, pub/sub latency spikes
- **Recovery**:
  - Redis Sentinel promotes slave to master (automatic, ~1-2 second downtime)
  - WebSocket servers reconnect to new Redis master
  - Notifications persisted to DB during outage, redelivered after recovery
- **Manual Intervention**: If Sentinel fails, manually promote slave and update config

---

**Scenario 3: Client Connection Drop**
- **Trigger**: User's network interruption, mobile app backgrounded
- **Impact**: Single user disconnected
- **Detection**: Missing heartbeat, socket disconnect event
- **Recovery**:
  - Socket.io client auto-reconnects with exponential backoff
  - Server retrieves undelivered notifications from DB on reconnect
  - User sees missed notifications
- **Manual Intervention**: None (fully automatic)

---

#### Retry Strategy

**Transient Failures** (Redis, DB connection errors):
- **Retry attempts**: 3
- **Backoff**: Exponential (100ms, 200ms, 400ms)
- **Timeout**: 1 second per attempt
- **Fallback**: Log error, persist to DB for background retry

**Permanent Failures** (Invalid user_id, malformed notification):
- **No retry**: Log as error with full context
- **Dead letter queue**: Not needed (notifications are ephemeral)

#### Circuit Breakers

**Redis Connection**:
- **Threshold**: 5 consecutive failures
- **Timeout**: 30 seconds (half-open state after 30s)
- **Fallback**: Continue operating but notifications not routed across servers

**Database Connection**:
- **Threshold**: 10 consecutive failures
- **Timeout**: 60 seconds
- **Fallback**: Skip persistence (notifications still delivered if Redis works)

#### Graceful Degradation

**Redis Down**:
- WebSocket servers continue accepting connections
- Notifications only work for users on same server as error handler
- User experience: Reduced notification reliability (70% instead of 100%)

**Database Down**:
- Real-time notifications continue via Redis
- Connection history and notification audit trail not persisted
- User experience: No visible impact (real-time still works)

**Load Balancer Down**:
- Direct client connections to specific WS server URLs (requires client update)
- Reduced redundancy but system remains operational

---

### Monitoring & Observability

#### Metrics to Track

**System Metrics** (per WebSocket server):
- CPU utilization (target: <20% normal, <50% peak)
- Memory usage (target: <500MB per server)
- Network I/O (bytes in/out per second)
- Event loop lag (Node.js specific, target: <10ms)

**Application Metrics**:
- Active connections count (per server and total)
- Connections established per minute
- Connections dropped per minute
- Notification delivery latency (p50, p95, p99)
- Notification throughput (per second)
- Redis pub/sub latency
- Authentication success/failure rate

**Business Metrics**:
- Average notifications per user per hour
- Peak concurrent connections
- Connection duration (median, p95)
- Reconnection rate

#### Logging Strategy

**Log Levels**:
- **ERROR**: Connection failures, Redis/DB errors, authentication failures
- **WARN**: Reconnections, circuit breaker trips, rate limit violations
- **INFO**: Connection established/dropped, notifications sent
- **DEBUG**: Heartbeats, message routing (disabled in production)

**Structured Logging** (JSON format):
```json
{
  "timestamp": "2026-01-20T13:00:00Z",
  "level": "INFO",
  "service": "websocket-server",
  "server_id": "ws-server-1",
  "event": "connection_established",
  "user_id": "user-123",
  "socket_id": "abc123",
  "client_type": "web",
  "ip": "192.168.1.100"
}
```

**Log Aggregation**: Ship logs to centralized system (ELK, DataDog, CloudWatch Logs)

**Retention**: 30 days hot storage, 90 days archive

#### Alerting

**Critical Alerts** (PagerDuty/On-call):
- Redis down > 1 minute
- 50% or more WebSocket servers unhealthy
- Authentication service down
- Notification delivery latency > 1 second for 5 minutes

**Warning Alerts** (Slack/Email):
- Single WebSocket server unhealthy
- Connection count > 80% capacity on any server
- Notification delivery latency > 500ms for 10 minutes
- Redis connection pool exhausted
- Reconnection rate > 20% of active connections

#### Distributed Tracing

**Tool**: Jaeger or DataDog APM

**Trace Critical Path**:
1. Error occurs in error handler
2. Documentation service called
3. Notification published to Redis
4. Redis routes to WebSocket server
5. Notification sent to client

**Trace ID**: Generated in error handler, propagated through Redis message, included in client notification

---

### Testing Strategy

#### Unit Testing

**Scope**: 
- WebSocket server: Connection handling, authentication, message routing
- Notification service: Publishing logic, Redis interaction
- Integration with error handler: Notification triggering

**Coverage Target**: 80%

**Tools**: Jest, Sinon for mocking Redis/Socket.io

**Key Tests**:
- Authentication with valid/invalid JWT
- Notification routing to correct socket
- Connection state persistence
- Error handling for Redis failures

---

#### Integration Testing

**Scope**: 
- End-to-end flow: Error → Documentation → Notification → Client
- Redis pub/sub across multiple servers
- Database persistence and retrieval
- Load balancer sticky session behavior

**Test Scenarios**:
- Connect client, trigger error, verify notification delivery
- Simulate Redis failure, verify graceful degradation
- Multiple clients for same user, verify all receive notification
- Client disconnect and reconnect, verify missed notifications

**Tools**: Supertest for HTTP/WebSocket, Redis test instance

---

#### Performance Testing

**Scope**: 
- Load test: 2,000+ concurrent connections
- Stress test: Find breaking point
- Latency test: Measure p95 notification delivery time
- Reconnection storm: 1,000 clients reconnect simultaneously

**Tools**: k6 with WebSocket support, custom load test scripts

**Acceptance Criteria**:
- 2,000 connections with <100ms delivery latency
- <5% connection failures under normal load
- Graceful degradation under overload (no crashes)

---

#### Security Testing

**Scope**:
- Authentication bypass attempts
- Unauthorized notification access
- DoS via connection flood
- Message injection attacks

**Tests**:
- Try connecting without valid JWT (should fail)
- Try receiving another user's notifications (should be blocked)
- Open 1,000 connections from single IP (should be rate limited)
- Send malformed messages (should be rejected)

**Tools**: OWASP ZAP for automated scanning, manual penetration testing

---

### Deployment Strategy

#### Deployment Method

**Strategy**: **Rolling Deployment** with health checks

**Rationale**: 
- Zero-downtime deployment for WebSocket servers
- Clients auto-reconnect if their server is restarted
- Can deploy during business hours

**Process**:
1. Deploy new version to 1 server
2. Wait for health check to pass
3. Shift traffic away from next server (stop accepting new connections)
4. Wait for existing connections to drain (or force disconnect after 30 seconds)
5. Deploy new version to that server
6. Repeat for remaining servers

**Rollback Time**: 5-10 minutes (redeploy previous version)

---

#### Infrastructure Changes

**New Resources**:
- **WebSocket Servers**: 2 EC2 instances (t3.medium) or equivalent
  - Node.js runtime, Socket.io, Express
  - 4 GB RAM, 2 vCPUs each
- **Redis Instance**: 1 Redis 6+ instance (cache.t3.medium)
  - 3.1 GB RAM, high availability mode (replication enabled)
- **Load Balancer**: Application Load Balancer (ALB) or Nginx
  - Sticky session configuration
  - Health check endpoint: `GET /health`

**Configuration Changes**:
- **Environment Variables**:
  ```
  WEBSOCKET_PORT=3001
  REDIS_URL=redis://redis.example.com:6379
  DATABASE_URL=postgres://user:pass@db.example.com:5432/mydb
  JWT_SECRET=<existing secret>
  MAX_CONNECTIONS=5000
  HEARTBEAT_INTERVAL=30000
  ```
- **Load Balancer**:
  - Sticky session cookie: `io` (Socket.io default)
  - Health check path: `/health`
  - Target group: WebSocket servers on port 3001

**Infrastructure as Code**: 
- Terraform for AWS resources
- Ansible for server configuration
- Docker containers for WebSocket servers (optional)

---

#### Migration Path

**From**: No real-time notification system
**To**: WebSocket-based real-time notifications

**Steps**:

1. **Phase 1: Infrastructure Setup** (Week 1)
   - Provision WebSocket servers and Redis
   - Configure load balancer with sticky sessions
   - Set up monitoring and alerting

2. **Phase 2: Backend Integration** (Week 2)
   - Deploy notification service
   - Integrate with error handler middleware
   - Deploy database migrations

3. **Phase 3: Gradual Rollout** (Week 3)
   - Enable for 10% of users (feature flag)
   - Monitor metrics and fix issues
   - Increase to 50% of users
   - Full rollout after validation

**Validation**:
- Connection success rate > 95%
- Notification delivery latency < 100ms (p95)
- No increase in error rate
- User feedback positive

---

#### Rollback Plan

**Triggers**: 
- Connection success rate < 90%
- Notification delivery failure > 10%
- System instability (crashes, high latency)
- Critical bugs discovered

**Steps**:
1. Disable feature flag (turns off WebSocket connections for new users)
2. Existing connections drain naturally (or force disconnect)
3. Redeploy previous version of WebSocket servers
4. Redeploy previous version of error handler (without notification publishing)
5. Verify system stability

**Data Handling**: 
- Database tables remain (contain only connection logs)
- No data loss (notifications stored in DB even if not delivered)
- Can re-enable once fix is deployed

**Time Estimate**: 10-15 minutes for full rollback

---

### Technical Risks & Mitigations

| Risk | Probability | Impact | Mitigation | Owner |
|------|-------------|--------|------------|-------|
| WebSocket server overwhelmed at scale | Medium | High | Horizontal scaling, load testing, auto-scaling | Backend Team |
| Redis becomes single point of failure | Medium | High | Redis Sentinel for HA, fallback to DB persistence | Infrastructure Team |
| Client reconnection storms after outage | High | Medium | Exponential backoff, connection rate limiting | Frontend Team |
| JWT token expiry during long sessions | High | Low | Client auto-renews token before expiry | Frontend Team |
| Database connection pool exhausted | Low | Medium | Async writes, connection pool tuning | Backend Team |
| Load balancer sticky sessions break | Low | High | Test thoroughly in staging, monitor connection stability | Infrastructure Team |
| Message payload too large causing slowdown | Low | Medium | Payload size validation (max 10KB), compression | Backend Team |

---

## 🔍 API Surface Analysis

**Detected API Components**:
- ✅ **1 WebSocket connection** detected (`wss://api.example.com/`)
- ✅ **Multiple message types**: Authentication, subscription, notification delivery, heartbeat
- **External facing**: Yes (web and mobile clients)
- **Authentication required**: Yes (JWT Bearer token)
- **Versioning needed**: No (WebSocket protocol is version agnostic, use message type versioning)
- **Rate limiting needed**: Yes (connection and message rate limits)

**Complexity**: **High** (Complex bidirectional protocol with authentication, multiple message types, and real-time requirements)

---

## 📋 Next Steps

### Implementation Readiness Checklist
- ✅ Architecture design complete
- ⏳ **API specifications needed** (WebSocket protocol details)
- ⏳ Database migrations reviewed
- ⏳ Infrastructure setup (WebSocket servers, Redis, load balancer)
- ⏳ Load testing plan executed

### Recommended Actions

1. **Review Architecture** 
   - [ ] Review this architecture document with the backend team
   - [ ] Validate Redis pub/sub approach with infrastructure team
   - [ ] Confirm horizontal scaling strategy with DevOps
   - [ ] Approve ADRs (Redis pub/sub, dual state storage, sticky sessions)

2. **✅ API Design Phase Recommended**
   - [ ] **Invoke API Design Agent** to create detailed WebSocket protocol specifications
   - **Command**: `@api-design design WebSocket APIs for issue #105 using this architecture`
   - **Reason**: This feature uses WebSocket with multiple message types and bidirectional communication. Detailed protocol specifications will ensure:
     - Consistent message formats
     - Proper error handling
     - Security patterns (authentication flow)
     - Client implementation guidance
     - OpenAPI documentation (AsyncAPI for WebSocket)

3. **Database Review**
   - [ ] Review migration plan with DBA team
   - [ ] Test migrations in staging environment
   - [ ] Plan rollback procedures
   - [ ] Set up cleanup jobs for old data

4. **Infrastructure Setup**
   - [ ] Provision WebSocket servers (2x EC2 t3.medium)
   - [ ] Set up Redis with Sentinel (HA mode)
   - [ ] Configure load balancer with sticky sessions
   - [ ] Set up monitoring (Prometheus, Grafana)
   - [ ] Configure alerting (PagerDuty, Slack)
   - [ ] Test auto-scaling policies

5. **Load Testing**
   - [ ] Set up k6 load testing environment
   - [ ] Execute normal load test (500 connections)
   - [ ] Execute peak load test (2,000 connections)
   - [ ] Execute stress test (5,000+ connections)
   - [ ] Validate latency requirements (<100ms p95)

6. **Ready for Implementation**
   - Once above items are complete, this architecture is ready for coding
   - Attach this document to implementation sub-issues
   - Reference ADRs when making implementation decisions
   - Update architecture based on implementation learnings

### Timeline Estimate
- Architecture review: 1-2 days
- **API design (WebSocket protocol)**: 1 day
- Database migrations: 1 day
- Infrastructure setup: 2-3 days
- Load testing: 2 days
- Implementation: 2-3 weeks
- **Total**: 3-4 weeks from architecture to production

---

## Summary

This architecture provides a scalable, resilient foundation for real-time notifications. Key highlights:

✅ **Scalable**: Horizontal scaling to 10,000+ connections via Redis pub/sub
✅ **Resilient**: Graceful degradation, auto-reconnect, circuit breakers
✅ **Performant**: <100ms delivery latency, optimized for real-time
✅ **Secure**: JWT authentication, rate limiting, channel-level authorization
✅ **Observable**: Comprehensive monitoring, structured logging, distributed tracing
✅ **Production-ready**: ADRs document key decisions, clear deployment strategy

**Next immediate action**: Invoke API Design Agent to specify WebSocket protocol details before implementation begins.
