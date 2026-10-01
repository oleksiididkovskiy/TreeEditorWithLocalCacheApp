import { Injectable, computed, signal } from '@angular/core';
import { ApplyChangesRequest, CachedNode } from '../../models/cached-node';
import { TreeNode } from '../../models/tree-node';

export interface CachedTreeNode extends CachedNode {
  children: CachedTreeNode[];
}

@Injectable({ providedIn: 'root' })
export class CacheService {
  private readonly nodes = new Map<string, CachedNode>();
  private readonly version = signal(0);
  readonly revision = this.version.asReadonly();

  readonly roots = computed(() => this.buildRoots());
  readonly orphans = computed(() => this.buildOrphans());
  readonly size = computed(() => { this.version(); return this.nodes.size; });

  get(id: string): CachedNode | undefined {
    return this.nodes.get(id);
  }

  addNew(node: TreeNode): void {
    this.nodes.set(node.id, {
      id: node.id,
      parentId: node.parentId,
      value: node.value,
      state: 'new'
    });
    this.bump();
  }

  load(node: TreeNode): void {
    const current = this.nodes.get(node.id);
    if (current && current.state !== 'unchanged') {
      return;
    }

    this.nodes.set(node.id, {
      id: node.id,
      parentId: node.parentId,
      value: node.value,
      state: current?.state ?? 'unchanged'
    });
    this.bump();
  }

  update(id: string, value: string): void {
    const node = this.nodes.get(id);
    if (!node || node.state === 'deleted') return;

    node.value = value;
    if (node.state === 'unchanged') node.state = 'modified';
    this.bump();
  }

  addChild(parentId: string, value: string): string {
    const parent = this.nodes.get(parentId);
    if (!parent || parent.state === 'deleted') {
      throw new Error('Cannot add a child to a deleted or uncached parent.');
    }

    const id = crypto.randomUUID();
    this.nodes.set(id, {
      id,
      parentId,
      value,
      state: 'new'
    });
    this.newIds.add(id);
    this.bump();
    return id;
  }

  delete(id: string): void {
    const root = this.nodes.get(id);
    if (!root || root.state === 'deleted') return;

    for (const node of this.nodes.values()) {
      if (this.isDescendantOf(node, id)) {
        node.state = 'deleted';
      }
    }
    root.state = 'deleted';
    this.bump();
  }

  clear(): void {
    this.nodes.clear();
    this.newIds.clear();
    this.bump();
  }

  toApplyRequest(): ApplyChangesRequest {
    const updates: ApplyChangesRequest['updates'] = [];
    const creates: ApplyChangesRequest['creates'] = [];
    const deletes: string[] = [];

    for (const node of this.nodes.values()) {
      if (node.state === 'modified') {
        updates.push({ id: node.id, value: node.value });
      } else if (node.state === 'new') {
        creates.push({ id: node.id, parentId: node.parentId, value: node.value });
      } else if (node.state === 'deleted') {
        if (this.isPersisted(node)) {
          deletes.push(node.id);
        }
      }
    }

    return { updates, creates, deletes };
  }

  hasPendingChanges(): boolean {
    return [...this.nodes.values()].some((node) => node.state !== 'unchanged');
  }

  private isPersisted(node: CachedNode): boolean {
    return !this.newIds.has(node.id);
  }

  private readonly newIds = new Set<string>();

  private buildRoots(): CachedTreeNode[] {
    this.version();
    const byParent = this.groupByParent();
    return (byParent.get(null) ?? []).map((node) => this.withChildren(node, byParent));
  }

  private buildOrphans(): CachedTreeNode[] {
    this.version();
    const byParent = this.groupByParent();
    const result: CachedTreeNode[] = [];
    for (const node of this.nodes.values()) {
      if (node.parentId !== null && !this.nodes.has(node.parentId)) {
        result.push(this.withChildren(node, byParent));
      }
    }
    return result.sort((a, b) => a.value.localeCompare(b.value));
  }

  private groupByParent(): Map<string | null, CachedNode[]> {
    const map = new Map<string | null, CachedNode[]>();
    for (const node of this.nodes.values()) {
      const bucket = map.get(node.parentId) ?? [];
      bucket.push(node);
      map.set(node.parentId, bucket);
    }
    for (const bucket of map.values()) {
      bucket.sort((a, b) => a.value.localeCompare(b.value));
    }
    return map;
  }

  private withChildren(node: CachedNode, byParent: Map<string | null, CachedNode[]>): CachedTreeNode {
    return {
      ...node,
      children: (byParent.get(node.id) ?? []).map((child) => this.withChildren(child, byParent))
    };
  }

  private isDescendantOf(node: CachedNode, ancestorId: string): boolean {
    let parentId = node.parentId;
    while (parentId) {
      if (parentId === ancestorId) return true;
      parentId = this.nodes.get(parentId)?.parentId ?? null;
    }
    return false;
  }

  private bump(): void {
    this.version.update((value) => value + 1);
  }
}
