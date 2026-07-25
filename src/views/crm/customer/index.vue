<template>
  <div class="customer-page">
    <QueryPage api="/admin/crm/customers" :search-param="searchForm" @reset="handleReset" ref="queryPageRef">
      <template #actions>
        <el-button type="primary" :icon="Plus" @click="handleAdd">新增客户</el-button>
      </template>

      <template #searchConditions>
        <el-form-item label="客户名称">
          <el-input v-model="searchForm.customerName" placeholder="请输入客户名称" />
        </el-form-item>
        <el-form-item label="客户类型">
          <el-select v-model="searchForm.customerType" placeholder="请选择客户类型">
            <el-option label="个人客户" value="个人客户" />
            <el-option label="企业客户" value="企业客户" />
          </el-select>
        </el-form-item>
        <el-form-item label="客户等级">
          <el-select v-model="searchForm.grade" placeholder="请选择客户等级">
            <el-option label="A级" value="A" />
            <el-option label="B级" value="B" />
            <el-option label="C级" value="C" />
            <el-option label="D级" value="D" />
          </el-select>
        </el-form-item>
        <el-form-item label="客户状态">
          <el-select v-model="searchForm.status" placeholder="请选择客户状态">
            <el-option label="待联系" value="待联系" />
            <el-option label="跟进中" value="跟进中" />
            <el-option label="已成交" value="已成交" />
            <el-option label="已流失" value="已流失" />
          </el-select>
        </el-form-item>
      </template>

      <template #table="scope">
        <el-table :data="scope.tableData" :row-key="'id'" :row-class-name="getRowClassName" border>
          <el-table-column prop="customerName" label="客户名称" min-width="150" />
          <el-table-column prop="customerType" label="客户类型" width="100" />
          <el-table-column prop="mainProduct" label="主营产品" min-width="120" />
          <el-table-column prop="grade" label="客户等级" width="80">
            <template #default="scope">
              <el-tag :type="getGradeType(scope.row.grade)">{{ scope.row.grade }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="score" label="客户积分" width="80" />
          <el-table-column prop="city" label="城市" width="100" />
          <el-table-column prop="status" label="状态" width="90">
            <template #default="scope">
              <el-tag :type="getStatusType(scope.row.status)">{{ scope.row.status }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="createdAt" label="创建时间" width="160">
            <template #default="scope">
              {{ formatDate(scope.row.createdAt) }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="200" fixed="right">
            <template #default="scope">
              <el-button size="small" :icon="Edit" @click="handleEdit(scope.row)">编辑</el-button>
              <el-button size="small" type="danger" :icon="Delete" @click="handleDelete(scope.row)">删除</el-button>
              <el-button size="small" :icon="View" @click="handleView(scope.row)">详情</el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
    </QueryPage>

    <!-- 新增/编辑弹窗 -->
    <el-dialog :title="isEdit ? '编辑客户' : '新增客户'" v-model="dialogVisible" width="600px">
      <el-form :model="form" label-width="100px">
        <el-form-item label="客户名称" prop="customerName">
          <el-input v-model="form.customerName" placeholder="请输入客户名称" />
        </el-form-item>
        <el-form-item label="客户类型" prop="customerType">
          <el-select v-model="form.customerType" placeholder="请选择客户类型">
            <el-option label="个人客户" value="个人客户" />
            <el-option label="企业客户" value="企业客户" />
          </el-select>
        </el-form-item>
        <el-form-item label="主营产品" prop="mainProduct">
          <el-input v-model="form.mainProduct" placeholder="请输入主营产品" />
        </el-form-item>
        <el-form-item label="客户等级" prop="grade">
          <el-select v-model="form.grade" placeholder="请选择客户等级">
            <el-option label="A级" value="A" />
            <el-option label="B级" value="B" />
            <el-option label="C级" value="C" />
            <el-option label="D级" value="D" />
          </el-select>
        </el-form-item>
        <el-form-item label="客户积分" prop="score">
          <el-input-number v-model="form.score" :min="0" />
        </el-form-item>
        <el-form-item label="省份" prop="province">
          <el-input v-model="form.province" placeholder="请输入省份" />
        </el-form-item>
        <el-form-item label="城市" prop="city">
          <el-input v-model="form.city" placeholder="请输入城市" />
        </el-form-item>
        <el-form-item label="区县" prop="area">
          <el-input v-model="form.area" placeholder="请输入区县" />
        </el-form-item>
        <el-form-item label="详细地址" prop="address">
          <el-input v-model="form.address" placeholder="请输入详细地址" />
        </el-form-item>
        <el-form-item label="来源平台" prop="sourcePlatform">
          <el-input v-model="form.sourcePlatform" placeholder="请输入来源平台" />
        </el-form-item>
        <el-form-item label="客户状态" prop="status">
          <el-select v-model="form.status" placeholder="请选择客户状态">
            <el-option label="待联系" value="待联系" />
            <el-option label="跟进中" value="跟进中" />
            <el-option label="已成交" value="已成交" />
            <el-option label="已流失" value="已流失" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input type="textarea" v-model="form.remark" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <div slot="footer" class="dialog-footer">
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">{{ isEdit ? '修改' : '确定' }}</el-button>
      </div>
    </el-dialog>

    <!-- 详情弹窗 -->
    <el-dialog title="客户详情" v-model="detailVisible" width="700px">
      <div v-if="detailData" class="detail-content">
        <div class="detail-section">
          <h4 class="section-title">基本信息</h4>
          <el-descriptions :column="2" border>
            <el-descriptions-item label="客户名称">{{ detailData.customerName }}</el-descriptions-item>
            <el-descriptions-item label="客户类型">{{ detailData.customerType }}</el-descriptions-item>
            <el-descriptions-item label="主营产品">{{ detailData.mainProduct }}</el-descriptions-item>
            <el-descriptions-item label="客户等级">
              <el-tag :type="getGradeType(detailData.grade)">{{ detailData.grade }}</el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="客户积分">{{ detailData.score }}</el-descriptions-item>
            <el-descriptions-item label="客户状态">
              <el-tag :type="getStatusType(detailData.status)">{{ detailData.status }}</el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="来源平台">{{ detailData.sourcePlatform }}</el-descriptions-item>
            <el-descriptions-item label="来源线索ID">{{ detailData.sourceLeadId || '-' }}</el-descriptions-item>
          </el-descriptions>
        </div>
        <div class="detail-section">
          <h4 class="section-title">地址信息</h4>
          <el-descriptions :column="2" border>
            <el-descriptions-item label="省份">{{ detailData.province }}</el-descriptions-item>
            <el-descriptions-item label="城市">{{ detailData.city }}</el-descriptions-item>
            <el-descriptions-item label="区县">{{ detailData.area }}</el-descriptions-item>
            <el-descriptions-item label="详细地址">{{ detailData.address }}</el-descriptions-item>
            <el-descriptions-item label="纬度">{{ detailData.lat || '-' }}</el-descriptions-item>
            <el-descriptions-item label="经度">{{ detailData.lng || '-' }}</el-descriptions-item>
          </el-descriptions>
        </div>
        <div class="detail-section">
          <h4 class="section-title">其他信息</h4>
          <el-descriptions :column="2" border>
            <el-descriptions-item label="上级客户">{{ detailData.parentCustomerName || '-' }}</el-descriptions-item>
            <el-descriptions-item label="负责人">{{ detailData.ownerUserName || '-' }}</el-descriptions-item>
            <el-descriptions-item label="备注">{{ detailData.remark || '-' }}</el-descriptions-item>
            <el-descriptions-item label="创建时间">{{ formatDate(detailData.createdAt) }}</el-descriptions-item>
            <el-descriptions-item label="更新时间">{{ formatDate(detailData.updatedAt) }}</el-descriptions-item>
          </el-descriptions>
        </div>
      </div>
      <div slot="footer" class="dialog-footer">
        <el-button @click="detailVisible = false">关闭</el-button>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts" name="customer">
import { ref, reactive } from "vue";
import { Plus, Edit, Delete, View } from "@element-plus/icons-vue";
import { ElMessage, ElMessageBox } from "element-plus";
import QueryPage from "@/components/QueryPage/index.vue";
import { crmCustomerApi } from "@/api/modules/crmCustomer";

const queryPageRef = ref();
const dialogVisible = ref(false);
const detailVisible = ref(false);
const isEdit = ref(false);

interface CustomerDetail {
  id: string;
  customerName: string;
  customerType: string;
  mainProduct: string;
  grade: string;
  score: number;
  province: string;
  city: string;
  area: string;
  address: string;
  lat: number | null;
  lng: number | null;
  sourcePlatform: string;
  sourceLeadId: number | null;
  status: string;
  parentCustomerName: string | null;
  ownerUserName: string | null;
  remark: string;
  createdAt: string;
  updatedAt: string;
}

const detailData = ref<CustomerDetail | null>(null);

const searchForm = reactive({
  customerName: "",
  customerType: "",
  grade: "",
  status: ""
});

const form = reactive({
  id: "",
  customerName: "",
  customerType: "",
  mainProduct: "",
  grade: "",
  score: 0,
  province: "",
  city: "",
  area: "",
  address: "",
  lat: undefined,
  lng: undefined,
  sourcePlatform: "",
  sourceLeadId: undefined,
  status: "待联系",
  ownerUserId: undefined,
  remark: "",
  parentCustomerId: undefined,
});

const getRowClassName = (row: any) => {
  if (row.status === "已流失") {
    return "row-disabled";
  }
  return "";
};

const getStatusType = (status: string) => {
  const types: Record<string, string> = {
    "待联系": "info",
    "跟进中": "warning",
    "已成交": "success",
    "已流失": "danger",
  };
  return types[status] || "default";
};

const getGradeType = (grade: string) => {
  const types: Record<string, string> = {
    "A": "danger",
    "B": "warning",
    "C": "info",
    "D": "default",
  };
  return types[grade] || "default";
};

const formatDate = (date: string) => {
  if (!date) return "-";
  return new Date(date).toLocaleString("zh-CN");
};

const handleReset = () => {
  searchForm.customerName = "";
  searchForm.customerType = "";
  searchForm.grade = "";
  searchForm.status = "";
};

const handleAdd = () => {
  isEdit.value = false;
  Object.keys(form).forEach(key => {
    (form as any)[key] = "";
  });
  form.status = "待联系";
  form.score = 0;
  dialogVisible.value = true;
};

const handleEdit = (row: any) => {
  isEdit.value = true;
  Object.assign(form, row);
  dialogVisible.value = true;
};

const handleView = async (row: any) => {
  const res = await crmCustomerApi.getCustomer(row.id);
  detailData.value = res.data.data;
  detailVisible.value = true;
};

const handleDelete = async (row: any) => {
  const confirm = await ElMessageBox.confirm(
    `确定要删除客户「${row.customerName}」吗？`,
    "提示",
    { type: "warning" }
  );
  if (confirm === "confirm") {
    await crmCustomerApi.deleteCustomer(row.id);
    ElMessage.success("删除成功");
    queryPageRef.value?.getTableList();
  }
};

const handleSubmit = async () => {
  if (!form.customerName) {
    ElMessage.error("请输入客户名称");
    return;
  }

  try {
    if (isEdit.value) {
      await crmCustomerApi.updateCustomer(form.id, form);
      ElMessage.success("修改成功");
    } else {
      await crmCustomerApi.createCustomer(form);
      ElMessage.success("创建成功");
    }
    dialogVisible.value = false;
    queryPageRef.value?.getTableList();
  } catch (error) {
    ElMessage.error("操作失败");
  }
};
</script>

<style scoped lang="scss">
.row-disabled {
  opacity: 0.6;
}

.detail-content {
  padding: 16px;
}

.detail-section {
  margin-bottom: 20px;

  .section-title {
    font-size: 15px;
    font-weight: 600;
    margin-bottom: 12px;
    padding-left: 8px;
    border-left: 3px solid var(--el-color-primary);
  }
}

.dialog-footer {
  text-align: right;
}
</style>