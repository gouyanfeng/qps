import http from "@/api";

const tagApi = {
  // 获取标签列表
  getTagList(params: any) {
    return http.get("/admin/tags", params);
  },

  // 获取单个标签详情
  getTagById(id: string) {
    return http.get(`/admin/tags/${id}`);
  },

  // 创建标签
  createTag(data: { name: string; color?: string }) {
    return http.post("/admin/tags", data);
  },

  // 更新标签
  updateTag(id: string, data: { name?: string; color?: string }) {
    return http.put(`/admin/tags/${id}`, data);
  },

  // 删除标签
  deleteTag(id: string) {
    return http.delete(`/admin/tags/${id}`);
  },
};

export { tagApi };
