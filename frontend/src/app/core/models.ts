export type UserRole = 'Admin' | 'Agent';
export type DurationType = 'FullDay' | 'PartialDay';
export type RequestStatus = 'Pending' | 'Approved' | 'Denied';

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  role: UserRole;
}

export interface AuthResponse {
  token: string;
  user: UserProfile;
}

export interface CreditBalance {
  earnedCredits: number;
  takenCredits: number;
  availableBalance: number;
  pendingReservedHours: number;
  requestableBalance: number;
}

export interface AgentOverview {
  id: string;
  name: string;
  email: string;
  credits: CreditBalance;
}

export interface TimeOffRequest {
  id: string;
  agentId: string;
  agentName: string;
  startDate: string;
  endDate: string;
  durationType: DurationType;
  requestedHours: number;
  reason: string | null;
  status: RequestStatus;
  createdAt: string;
  updatedAt: string;
  reviewedBy: string | null;
  reviewedAt: string | null;
}

export interface CreateTimeOffRequest {
  startDate: string;
  endDate: string;
  durationType: DurationType;
  requestedHours?: number | null;
  reason?: string | null;
}

export interface ApiErrorBody {
  error?: string;
  message?: string;
}
