export interface TreeNode {
  id: string;
  parentId: string | null;
  value: string;
}

export interface DbTreeNode extends TreeNode {
  children: DbTreeNode[];
  expanded: boolean;
  childrenLoaded: boolean;
  loading: boolean;
}
