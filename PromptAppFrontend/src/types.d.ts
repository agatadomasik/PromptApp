// types.ts
export interface Prompt {
  id: string;
  content: string;
  state: string;
  result: string | null;
}

export interface PromptRequest {
  content: string;
}