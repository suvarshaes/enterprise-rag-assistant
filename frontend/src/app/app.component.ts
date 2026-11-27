import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpEventType, HttpParams } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { AskResponse, CitationItem, DocumentItem, UploadResult } from './rag.models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  private http = inject(HttpClient);
  private apiBase = 'http://localhost:5000/api';

  selectedDomain = 'ClinicalTrial';
  fileToUpload: File | null = null;
  uploading = false;
  uploadStage: 'uploading' | 'indexing' | null = null;
  uploadProgressPercent: number | null = null;
  asking = false;
  documentsLoading = true;

  documents: DocumentItem[] = [];
  filterDocumentId: string | null = null;
  lastUploadedDocumentId: string | null = null;
  question = '';
  submittedQuestion = '';
  response: AskResponse | null = null;
  activeCitation: CitationItem | null = null;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  documentsError: string | null = null;

  private selectedFileInput: HTMLInputElement | null = null;

  get questionScopeDocuments(): DocumentItem[] {
    return this.documents.filter((document) =>
      document.domain === this.selectedDomain && document.status === 'Ready'
    );
  }

  get selectedDocument(): DocumentItem | undefined {
    return this.questionScopeDocuments.find((document) => document.documentId === this.filterDocumentId);
  }

  get canAskQuestion(): boolean {
    return this.filterDocumentId ? !!this.selectedDocument : this.questionScopeDocuments.length > 0;
  }

  onDomainChanged(domain: string) {
    this.selectedDomain = domain;
    if (this.filterDocumentId && !this.questionScopeDocuments.some((document) => document.documentId === this.filterDocumentId)) {
      this.filterDocumentId = null;
    }
  }

  ngOnInit() {
    this.loadDocuments();
  }

  loadDocuments(selectDocumentId?: string) {
    this.documentsLoading = true;
    this.documentsError = null;

    this.http.get<DocumentItem[]>(`${this.apiBase}/documents`).subscribe({
      next: (docs) => {
        this.documents = docs;
        if (selectDocumentId && docs.some((doc) => doc.documentId === selectDocumentId)) {
          this.filterDocumentId = selectDocumentId;
        }
        this.documentsLoading = false;
      },
      error: (err: unknown) => {
        this.documentsLoading = false;
        this.documentsError = this.getRequestErrorMessage(err, 'load your documents');
        console.error('Unable to load documents', err);
      }
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.selectedFileInput = input;
    this.errorMessage = null;
    this.successMessage = null;

    const file = input.files?.[0] ?? null;
    if (file && !file.name.toLowerCase().endsWith('.pdf')) {
      this.fileToUpload = null;
      input.value = '';
      this.errorMessage = 'Please choose a PDF file.';
      return;
    }

    this.fileToUpload = file;
  }

  uploadFile() {
    if (!this.fileToUpload) {
      this.errorMessage = 'Choose a PDF file before uploading.';
      return;
    }

    this.uploading = true;
    this.uploadStage = 'uploading';
    this.uploadProgressPercent = null;
    this.errorMessage = null;
    this.successMessage = null;

    const uploadedFileName = this.fileToUpload.name;
    const formData = new FormData();
    formData.append('file', this.fileToUpload);

    const params = new HttpParams().set('domain', this.selectedDomain);

    this.http.post<UploadResult>(`${this.apiBase}/documents/upload`, formData, {
      params,
      observe: 'events',
      reportProgress: true
    }).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress) {
          this.uploadProgressPercent = event.total
            ? Math.round((event.loaded / event.total) * 100)
            : null;
          if (event.total && event.loaded >= event.total) {
            this.uploadStage = 'indexing';
            this.uploadProgressPercent = null;
          }
          return;
        }

        if (event.type !== HttpEventType.Response) return;
        if (!event.body) {
          this.uploading = false;
          this.uploadStage = null;
          this.errorMessage = 'The API finished the request but returned no upload result. Reload your documents to check whether indexing completed.';
          return;
        }

        const result = event.body;
        this.uploading = false;
        this.uploadStage = null;
        this.uploadProgressPercent = null;
        this.fileToUpload = null;
        if (this.selectedFileInput) this.selectedFileInput.value = '';
        this.lastUploadedDocumentId = result.documentId;
        this.filterDocumentId = result.documentId;
        this.successMessage = `${uploadedFileName} was uploaded and indexed successfully. Document ID: ${result.documentId}`;
        this.loadDocuments(result.documentId);
      },
      error: (err: unknown) => {
        this.uploading = false;
        this.uploadStage = null;
        this.uploadProgressPercent = null;
        this.errorMessage = this.getRequestErrorMessage(err, 'upload the PDF');
        console.error('Unable to upload document', err);
      }
    });
  }

  askQuestion() {
    if (!this.question.trim()) {
      this.errorMessage = 'Enter a question before submitting.';
      return;
    }
    if (!this.canAskQuestion) {
      this.errorMessage = `There are no ready documents in the selected ${this.selectedDomain === 'ClinicalTrial' ? 'clinical trial' : 'financial report'} scope. Upload and index a document first, or choose another scope.`;
      return;
    }

    this.asking = true;
    this.errorMessage = null;
    this.successMessage = null;
    this.submittedQuestion = this.question.trim();
    this.response = null;
    this.activeCitation = null;

    this.http.post<AskResponse>(`${this.apiBase}/chat/ask`, {
      question: this.submittedQuestion,
      domain: this.filterDocumentId ? null : this.selectedDomain,
      documentId: this.filterDocumentId,
      topK: 5
    }).subscribe({
      next: (res) => {
        this.asking = false;
        this.response = res;
        this.activeCitation = res.citations[0] ?? null;
      },
      error: (err: unknown) => {
        this.asking = false;
        this.errorMessage = this.getRequestErrorMessage(err, 'get an answer');
        console.error('Unable to get answer', err);
      }
    });
  }

  private getRequestErrorMessage(error: unknown, action: string): string {
    if (!(error instanceof HttpErrorResponse)) {
      return `We couldn't ${action}. Please try again.`;
    }

    const serverMessage = this.getServerMessage(error.error);
    if (serverMessage) return `We couldn't ${action}: ${serverMessage}`;

    if (error.status === 0) {
      return `We couldn't ${action} because the browser couldn't reach the API. Check that RagApi is running at http://localhost:5000 and that the browser allows requests from the Angular app.`;
    }
    if (error.status === 400 || error.status === 422) {
      return `We couldn't ${action} because some of the submitted information was invalid. Check it and try again.`;
    }
    if (error.status === 401 || error.status === 403) {
      return `We couldn't ${action} because access was denied.`;
    }
    if (error.status === 404) {
      return `We couldn't ${action} because the requested resource wasn't found.`;
    }
    if (error.status === 413) {
      return 'This PDF is too large to upload. Choose a smaller file and try again.';
    }
    if (error.status === 415) {
      return 'This file type is not supported. Choose a PDF and try again.';
    }
    if (error.status >= 500) {
      return `The API received the request but could not complete it (server error ${error.status}). Check that SQL Server and the Python RAG service are running, then retry.`;
    }

    return `We couldn't ${action} (server error ${error.status}). Please check the submitted information and try again.`;
  }

  private getServerMessage(payload: unknown): string | null {
    if (typeof payload === 'string') return payload.trim() || null;
    if (!payload || typeof payload !== 'object' || Array.isArray(payload)) return null;

    const body = payload as Record<string, unknown>;
    const validationErrors = body['errors'];
    if (validationErrors && typeof validationErrors === 'object' && !Array.isArray(validationErrors)) {
      const messages = Object.values(validationErrors as Record<string, unknown>)
        .flatMap((value) => Array.isArray(value) ? value : [value])
        .filter((value): value is string => typeof value === 'string' && !!value.trim());
      if (messages.length) return messages.join(' ');
    }

    for (const key of ['detail', 'message', 'title']) {
      const value = body[key];
      if (typeof value === 'string' && value.trim()) return value.trim();
    }

    return null;
  }
}
