import type {
  CreateParams,
  DataProvider,
  DeleteParams,
  DeleteManyParams,
  GetManyParams,
  GetManyReferenceParams,
  GetListParams,
  GetOneParams,
  Identifier,
  RaRecord,
  UpdateParams,
  UpdateManyParams,
} from 'react-admin';

import { getAccessToken } from './auth/keycloak';

type JsonRecord = RaRecord & Record<string, unknown>;

const resourcePathMap: Record<string, string> = {
  products: '/api/backoffice/catalog/v1/products',
  brands: '/api/backoffice/catalog/v1/brands',
  categories: '/api/backoffice/catalog/v1/categories',
  collections: '/api/backoffice/catalog/v1/collections',
};

function normalizeResource(resource: string): string {
  return resource.replace(/^\/+/, '').replace(/\/+$/, '');
}

function resolveResourcePath(resource: string): string {
  const normalizedResource = normalizeResource(resource);
  return resourcePathMap[normalizedResource] ?? `/${normalizedResource}`;
}

function buildCollectionUrl(apiEntrypoint: string, resource: string, params?: GetListParams): string {
  const url = new URL(resolveResourcePath(resource), apiEntrypoint);
  const page = params?.pagination?.page ?? 1;
  const perPage = params?.pagination?.perPage ?? 25;
  url.searchParams.set('limit', String(perPage));
  url.searchParams.set('offset', String(Math.max(0, (page - 1) * perPage)));

  const searchTerm = params?.filter?.searchTerm ?? params?.filter?.q;
  if (typeof searchTerm === 'string' && searchTerm.trim()) {
    url.searchParams.set('searchTerm', searchTerm.trim());
  }

  return url.toString();
}

function buildItemUrl(apiEntrypoint: string, resource: string, id: string | number): string {
  const resourceUrl = new URL(resolveResourcePath(resource), apiEntrypoint);
  resourceUrl.pathname = `${resourceUrl.pathname.replace(/\/+$/, '')}/${encodeURIComponent(String(id))}`;
  return resourceUrl.toString();
}

async function requestJson(url: string, init: RequestInit = {}): Promise<unknown> {
  const token = await getAccessToken();
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');

  if (init.body) {
    headers.set('Content-Type', 'application/json');
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(url, { ...init, headers });
  if (!response.ok) {
    const error = new Error(`API request failed with status ${response.status}`) as Error & { status?: number };
    error.status = response.status;
    throw error;
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

function asRecord(value: unknown, fallbackId?: string | number): JsonRecord {
  if (value && typeof value === 'object') {
    const record = value as JsonRecord;
    if (record.id === undefined && fallbackId !== undefined) {
      return { ...record, id: fallbackId };
    }

    return record;
  }

  return { id: fallbackId ?? 'unknown' };
}

function asRecords(value: unknown): JsonRecord[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value.map((item, index) => asRecord(item, index));
}

export function createBackofficeDataProvider(apiEntrypoint: string): DataProvider {
  const provider = {
    getList: async (resource: string, params: GetListParams) => {
      const data = asRecords(await requestJson(buildCollectionUrl(apiEntrypoint, resource, params)));
      return {
        data,
        pageInfo: {
          hasPreviousPage: (params.pagination?.page ?? 1) > 1,
          hasNextPage: data.length === (params.pagination?.perPage ?? 25),
        },
      };
    },
    getOne: async (resource: string, params: GetOneParams) => ({
      data: asRecord(await requestJson(buildItemUrl(apiEntrypoint, resource, params.id)), params.id),
    }),
    getMany: async (resource: string, params: GetManyParams) => {
      const records = await Promise.all(
        params.ids.map(async (id: Identifier) => asRecord(await requestJson(buildItemUrl(apiEntrypoint, resource, id)), id)),
      );
      return { data: records };
    },
    getManyReference: async (resource: string, params: GetManyReferenceParams) => {
      const data = asRecords(await requestJson(buildCollectionUrl(apiEntrypoint, resource, params)));
      return {
        data,
        pageInfo: {
          hasPreviousPage: params.pagination.page > 1,
          hasNextPage: data.length === params.pagination.perPage,
        },
      };
    },
    update: async (resource: string, params: UpdateParams) => ({
      data: asRecord(
        await requestJson(buildItemUrl(apiEntrypoint, resource, params.id), {
          method: 'PUT',
          body: JSON.stringify(params.data),
        }),
        params.id,
      ),
    }),
    updateMany: async (resource: string, params: UpdateManyParams) => {
      await Promise.all(
        params.ids.map((id: Identifier) =>
          requestJson(buildItemUrl(apiEntrypoint, resource, id), {
            method: 'PUT',
            body: JSON.stringify(params.data),
          }),
        ),
      );
      return { data: params.ids };
    },
    create: async (resource: string, params: CreateParams) => ({
      data: asRecord(
        await requestJson(new URL(resolveResourcePath(resource), apiEntrypoint).toString(), {
          method: 'POST',
          body: JSON.stringify(params.data),
        }),
      ),
    }),
    delete: async (resource: string, params: DeleteParams) => {
      const result = await requestJson(buildItemUrl(apiEntrypoint, resource, params.id), { method: 'DELETE' });
      return { data: asRecord(result ?? params.previousData, params.id) };
    },
    deleteMany: async (resource: string, params: DeleteManyParams) => {
      await Promise.all(params.ids.map((id: Identifier) => requestJson(buildItemUrl(apiEntrypoint, resource, id), { method: 'DELETE' })));
      return { data: params.ids };
    },
  };

  return provider as DataProvider;
}
