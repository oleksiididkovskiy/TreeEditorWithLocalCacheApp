export type CacheNodeState = 'unchanged' | 'modified' | 'new' | 'deleted';

export interface CachedNode {
  id: string;
  parentId: string | null;
  value: string;
  state: CacheNodeState;
}

export interface ApplyChangesRequest {
  updates: { id: string; value: string }[];
  creates: { id: string; parentId: string | null; value: string }[];
  deletes: string[];
}
