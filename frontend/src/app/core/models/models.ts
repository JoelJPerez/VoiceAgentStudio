// ── Auth models ───────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  fullName: string;
  email: string;
  role: string;
  expiresAt: string;
}

export interface CurrentUser {
  fullName: string;
  email: string;
  role: string;
  token: string;
}

// ── Agent models ──────────────────────────────────────────────────────

export type AgentStatus = 'Active' | 'Inactive' | 'Draft';
export type AgentTone   = 'Professional' | 'Friendly' | 'Formal' | 'Empathetic';
export type LlmProvider = 'OpenAI' | 'Anthropic';

export interface AgentSummary {
  id: string;
  name: string;
  status: AgentStatus;
  tone: AgentTone;
  modelName: string;
  totalSessions: number;
  avgResolutionRate: number;
  createdAt: string;
}

export interface Agent {
  id: string;
  name: string;
  description: string;
  status: AgentStatus;
  tone: AgentTone;
  llmProvider: LlmProvider;
  modelName: string;
  temperature: number;
  maxTokens: number;
  systemPrompt: string;
  objective: string;
  companyContext: string;
  escalationKeywords: string;
  autoEscalate: boolean;
  totalSessions: number;
  avgResolutionRate: number;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateAgentRequest {
  name: string;
  description: string;
  tone: AgentTone;
  llmProvider: LlmProvider;
  modelName: string;
  temperature: number;
  maxTokens: number;
  systemPrompt: string;
  objective: string;
  companyContext: string;
  escalationKeywords: string;
  autoEscalate: boolean;
}

export interface UpdateAgentRequest extends CreateAgentRequest {
  status: AgentStatus;
}

// ── API response wrapper ──────────────────────────────────────────────

export interface ApiError {
  status: number;
  message: string;
  timestamp: string;
}
