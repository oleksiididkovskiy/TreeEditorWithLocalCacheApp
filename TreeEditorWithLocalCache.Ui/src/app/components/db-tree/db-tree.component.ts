import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output, inject, signal } from '@angular/core';
import { TreeApiService } from '../../core/services/tree-api.service';
import { DbTreeNode, TreeNode } from '../../models/tree-node';

@Component({
  selector: 'app-db-tree',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './db-tree.component.html',
  styleUrl: './db-tree.component.scss'
})
export class DbTreeComponent {
  private readonly api = inject(TreeApiService);
  readonly roots = signal<DbTreeNode[]>([]);
  readonly loading = signal(false);

  @Output() loadToCache = new EventEmitter<TreeNode>();
  @Output() loadNewRootToCache = new EventEmitter<void>();

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    this.api.getRoots().subscribe({
      next: (nodes) => this.roots.set(nodes.map((node) => this.wrap(node))),
      error: () => this.loading.set(false),
      complete: () => this.loading.set(false)
    });
  }

  toggle(node: DbTreeNode): void {
    if (node.expanded) {
      node.expanded = false;
      return;
    }

    if (node.childrenLoaded) {
      node.expanded = true;
      return;
    }

    node.loading = true;
    this.api.getChildren(node.id).subscribe({
      next: (children) => {
        node.children = children.map((child) => this.wrap(child));
        node.childrenLoaded = true;
        node.expanded = true;
      },
      complete: () => node.loading = false,
      error: () => node.loading = false
    });
  }

  private wrap(node: TreeNode): DbTreeNode {
    return {
      ...node,
      children: [],
      expanded: false,
      childrenLoaded: false,
      loading: false
    };
  }
}
