import http from "@/api";

/**
 * @description 数据字典模块
 */
export const dataDictionaryApi = {
  // 获取数据字典列表
  getDataDictionaryList: (params: any) => {
    return http.get<any>("/admin/data-dictionaries", params);
  },
  // 获取数据字典树形结构
  getDataDictionaryTree: () => {
    return http.get<any>("/admin/data-dictionaries/tree");
  },
  // 获取单个数据字典
  getDataDictionary: (id: string) => {
    return http.get<any>(`/admin/data-dictionaries/${id}`);
  },
  // 新增数据字典
  addDataDictionary: (params: any) => {
    return http.post<any>("/admin/data-dictionaries", params);
  },
  // 更新数据字典
  updateDataDictionary: (id: string, params: any) => {
    return http.put<any>(`/admin/data-dictionaries/${id}`, params);
  },
  // 删除数据字典
  deleteDataDictionary: (id: string) => {
    return http.delete(`/admin/data-dictionaries/${id}`);
  },
};

export default dataDictionaryApi;
