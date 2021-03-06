import { Ticket } from './ticket.model';

export interface Dispute {
  id?: number;
  emailAddress: string;
  courtLocation: string;
  lawyerPresent: boolean;
  interpreterRequired: boolean;
  interpreterLanguage: string;
  callWitness: boolean;
  certifyCorrect: boolean;
  counts?: Count[];

  statusCode?: string;
  status?: string;
  note?: string;
  ticketType?: string;

  ticket: Ticket;
}

export interface Count {
  id: number;
  countNo: number;
  statuteId: number;
  description: string;
  count?: string;
  count1A1?: string;
  count1A2?: string;
  reductionReason?: string;
  timeReason?: string;
  count1B1?: string;
  count1B2?: string;
}
