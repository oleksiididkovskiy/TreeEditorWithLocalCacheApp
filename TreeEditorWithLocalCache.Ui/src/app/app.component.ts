import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { CachedTreeComponent } from './components/cached-tree/cached-tree.component';
import { DbTreeComponent } from './components/db-tree/db-tree.component';
import { NodeEditorComponent } from './components/node-editor/node-editor.component';
import { ToolbarComponent } from './components/toolbar/toolbar.component';
import { CacheService } from './core/services/cache.service';
import { TreeApiService } from './core/services/tree-api.service';
import { TreeNode } from './models/tree-node';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ToolbarComponent, DbTreeComponent, CachedTreeComponent, NodeEditorComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  readonly cache = inject(CacheService);
  private readonly api = inject(TreeApiService);

  readonly selectedId = signal<string | null>(null);
  readonly selectedNode = computed(() => {
    this.cache.revision();
    const id = this.selectedId();
    return id ? this.cache.get(id) : undefined;
  });

  readonly pendingChanges = () => this.cache.hasPendingChanges();

  loadToCache(node: { id: string }): void {
    this.api.getNode(node.id).subscribe({
      next: (loaded) => {
        this.cache.load(loaded);
        this.selectedId.set(loaded.id);
      },
      error: (error) => alert(this.errorMessage(error))
    });
  }

  loadNewRootToCache(): void {
    const id = crypto.randomUUID();
    let node = { id: id, parentId: null, value: 'Root' } as TreeNode;
    this.cache.addNew(node);
    this.selectedId.set(node.id);
  }

  selectNode(node: { id: string }): void {
    this.selectedId.set(node.id);
  }

  updateValue(value: string): void {
    const id = this.selectedId();
    if (id) this.cache.update(id, value);
  }

  addChild(value: string): void {
    const id = this.selectedId();
    if (!id) return;
    try {
      const childId = this.cache.addChild(id, value);
      this.selectedId.set(childId);
    } catch (error) {
      alert(error instanceof Error ? error.message : 'Unable to add child.');
    }
  }

  deleteSelected(): void {
    const id = this.selectedId();
    if (!id) return;
    if (!confirm('Mark this node and all loaded descendants as deleted?')) return;
    this.cache.delete(id);
  }

  apply(): void {
    const request = this.cache.toApplyRequest();
    this.api.apply(request).subscribe({
      next: () => {
        this.cache.clear();
        this.selectedId.set(null);
        window.location.reload();
      },
      error: (error) => alert(this.errorMessage(error))
    });
  }

  reset(): void {
    if (!confirm('Reset the database and clear the local cache?')) return;
    this.api.reset().subscribe({
      next: () => {
        this.cache.clear();
        this.selectedId.set(null);
        window.location.reload();
      },
      error: (error) => alert(this.errorMessage(error))
    });
  }

  private errorMessage(error: any): string {
    return error?.error?.message ?? error?.message ?? 'Request failed.';
  }
}
