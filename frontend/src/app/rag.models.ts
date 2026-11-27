export interface DocumentItem {
  documentId: string;
  fileName: string;
  domain: string;
  status: string;
  pageCount: number;
  uploadedAt: string;
}

export interface UploadResult {
  documentId: string;
  status: string;
  pageCount: number;
  chunksIndexed: number;
}

export interface CitationItem {
  citation_id: string;
  chunk_id: string;
  document_id: string;
  file_name: string;
  page_number: number;
  section_title?: string | null;
  excerpt: string;
}

export interface AskResponse {
  answer: string;
  citations: CitationItem[];
}
