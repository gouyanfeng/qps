import { Upload } from "@/api/interface/index";
import http from "@/api";

/**
 * @name 文件上传模块
 */
// 图片上传
export const uploadImg = (params: FormData, scene?: string) => {
  const query = scene ? `?scene=${encodeURIComponent(scene)}` : "";
  return http.post<Upload.ResFileUrl>(`/admin/file/upload/img${query}`, params, {
    cancel: false,
  });
};