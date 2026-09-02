export interface Comment {
  id: string;
  authorName: string;
  body: string;
  createdDate: string;
}

export interface CreateCommentRequest {
  authorName: string;
  body: string;
}
