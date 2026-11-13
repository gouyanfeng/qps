import { ElMessage } from "element-plus"
import { uploadImg } from "@/api/modules/system/upload"
import { resolveFileUrl } from "@/utils"
import {
  Document,
  Text,
  Paragraph,
  Heading,
  Bold,
  Italic,
  Underline,
  Strike,
  BulletList,
  OrderedList,
  Blockquote,
  Link,
  Image as TiptapImage,
  Table,
  TextAlign,
  Color,
  Highlight,
  History,
  HorizontalRule,
  FormatClear,
  Fullscreen
} from "element-tiptap"

const uploadCampaignContentImage = async (file: File): Promise<string> => {
  if (!["image/jpeg", "image/png", "image/gif"].includes(file.type)) {
    ElMessage.warning("图片仅支持 JPG、PNG 或 GIF 格式")
    throw new Error("图文介绍仅支持上传图片")
  }
  if (file.size > 5 * 1024 * 1024) {
    ElMessage.warning("图片大小不能超过 5MB")
    throw new Error("图片超过 5MB")
  }

  const formData = new FormData()
  formData.append("file", file)
  const { data } = await uploadImg(formData, "activity")
  if (!data.fileUrl) {
    ElMessage.error("图片上传成功，但未返回图片地址")
    throw new Error("图片上传未返回地址")
  }
  return resolveFileUrl(data.fileUrl)
}

const imageOptions: Parameters<typeof TiptapImage.configure>[0] & {
  uploadRequest: (file: File) => Promise<string>
} = {
  uploadRequest: uploadCampaignContentImage
}

export const extensions = [
  Document,
  Text,
  Paragraph,
  Heading,
  Bold,
  Italic,
  Underline,
  Strike,
  BulletList,
  OrderedList,
  Blockquote,
  Link,
  TiptapImage.configure(imageOptions),
  Table,
  TextAlign.configure({ types: ["heading", "paragraph"] }),
  Color,
  Highlight,
  History,
  HorizontalRule,
  FormatClear,
  Fullscreen
]
