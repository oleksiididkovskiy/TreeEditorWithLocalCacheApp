import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApplyChangesRequest } from '../../models/cached-node';
import { TreeNode } from '../../models/tree-node';

@Injectable({ providedIn: 'root' })
export class TreeApiService {
  private readonly http = inject(HttpClient);

  getRoots() {
    return this.http.get<TreeNode[]>('/api/tree/roots');
  }

  getChildren(parentId: string) {
    return this.http.get<TreeNode[]>(`/api/tree/${parentId}/children`);
  }

  getNode(id: string) {
    return this.http.get<TreeNode>(`/api/tree/${id}`);
  }

  apply(request: ApplyChangesRequest) {
    return this.http.post<void>('/api/tree/apply', request);
  }

  reset() {
    return this.http.post<void>('/api/tree/reset', {});
  }
}
