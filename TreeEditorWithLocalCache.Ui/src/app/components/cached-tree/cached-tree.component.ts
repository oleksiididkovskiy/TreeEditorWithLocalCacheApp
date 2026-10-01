import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CachedTreeNode } from '../../core/services/cache.service';

@Component({
  selector: 'app-cached-tree',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cached-tree.component.html',
  styleUrl: './cached-tree.component.scss'
})
export class CachedTreeComponent {
  @Input({ required: true }) roots: CachedTreeNode[] = [];
  @Input({ required: true }) orphans: CachedTreeNode[] = [];
  @Input() selectedId: string | null = null;
  @Output() selectNode = new EventEmitter<CachedTreeNode>();
}
