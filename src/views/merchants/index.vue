<template>
    <div class="table-box">
        <ProTable ref="proTable" title="商户管理" :columns="columns" :request-api="getMerchantList"
            :search-col="{ xs: 1, sm: 2, md: 2, lg: 3, xl: 3 }">
            <!-- 表格 header 按钮 -->
            <template #tableHeader>
                <el-button type="primary" :icon="CirclePlus" @click="openDrawer('新增')">新增商户</el-button>
            </template>

            <!-- 表格操作 -->
            <template #operation="scope">
                <el-button type="primary" link :icon="View" @click="openDrawer('查看', scope.row)">查看</el-button>
                <el-button type="primary" link :icon="EditPen" @click="openDrawer('编辑', scope.row)">编辑</el-button>
            </template>
        </ProTable>

        <!-- 商户抽屉组件 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="120px">
                <el-form-item label="商户名称">
                    <el-input v-model="form.name" placeholder="请输入商户名称" />
                </el-form-item>
                <el-form-item label="联系电话">
                    <el-input v-model="form.phone" placeholder="请输入联系电话" />
                </el-form-item>
                <el-form-item label="过期日期">
                    <el-date-picker v-model="form.expiryDate" type="datetime" placeholder="选择过期日期"
                        style="width: 100%" />
                </el-form-item>
                <el-form-item label="状态">
                    <el-switch v-model="form.isActive" active-text="激活" inactive-text="禁用" />
                </el-form-item>
            </el-form>
            <template #footer>
                <span class="dialog-footer">
                    <el-button @click="dialogVisible = false">取消</el-button>
                    <el-button type="primary" @click="submitForm">确定</el-button>
                </span>
            </template>
        </el-dialog>
    </div>
</template>

<script setup lang="ts" name="merchants">
import { ref, reactive } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import { Merchant } from "@/api/interface";
import ProTable from "@/components/ProTable/index.vue";
import { CirclePlus, EditPen, View } from "@element-plus/icons-vue";
import { ProTableInstance, ColumnProps } from "@/components/ProTable/interface";
import { merchantApi } from "@/api/modules/merchant";
import { validateHeaderName } from "http";

// ProTable 实例
const proTable = ref<ProTableInstance>();

// 对话框状态
const dialogVisible = ref(false);
const dialogTitle = ref("");
const dialogType = ref("");
const currentMerchantId = ref("");

// 表单数据
const form = reactive<Merchant.ReqMerchantForm>({
    name: "",
    phone: "",
    expiryDate: new Date().toISOString(),
    isActive: true
});

// 表格配置项
const columns = reactive<ColumnProps<Merchant.ResMerchantList>[]>([

    {
        prop: "name",
        label: "商户名称",
        width: 180,
        search: { el: "input" }
    },
    {
        prop: "phone",
        label: "联系电话",
        width: 150,
        search: { el: "input" }
    },
    {
        prop: "expiryDate",
        label: "过期日期",
        width: 200,
        formatter: (row: Merchant.ResMerchantList) => {
            return new Date(row.expiryDate).toLocaleString();
        }
    },
    {
        prop: "isActive",
        label: "状态",
        width: 100,
        // tag: true,
        formatter: (row: Merchant.ResMerchantList) => {
            return row.isActive ? "激活" : "禁用";
        },
        search: {
            el: "select",
            props: {
                options: [
                    { label: "激活", value: true },
                    { label: "禁用", value: false }
                ]
            }
        }
    },
    {
        prop: "createdAt",
        label: "创建时间",
        width: 200,
        formatter: (row: Merchant.ResMerchantList) => {
            return new Date(row.createdAt).toLocaleString();
        }
    },
    { prop: "operation", label: "操作", width: 200, fixed: "right" }
]);


// 获取商户列表
const getMerchantList = (params: any) => {
    return merchantApi.getMerchantList(params);
};

// 打开抽屉
const openDrawer = (type: string, row?: Merchant.ResMerchantList) => {
    dialogTitle.value = type;
    dialogType.value = type;

    if (type === "新增") {
        // 重置表单
        Object.assign(form, {
            name: "",
            phone: "",
            expiryDate: new Date().toISOString(),
            isActive: true
        });
        currentMerchantId.value = "";
    } else if (row) {
        // 填充表单数据
        Object.assign(form, {
            name: row.name,
            phone: row.phone,
            expiryDate: row.expiryDate,
            isActive: row.isActive
        });
        currentMerchantId.value = row.id;
    }

    dialogVisible.value = true;
};

// 提交表单
const submitForm = async () => {
    try {
        if (dialogType.value === "新增") {
            await merchantApi.addMerchant(form);
            ElMessage.success("新增商户成功");
        } else if (dialogType.value === "编辑" && currentMerchantId.value) {
            await merchantApi.updateMerchant(currentMerchantId.value, form);
            ElMessage.success("更新商户成功");
        }
        dialogVisible.value = false;
        proTable.value?.getTableList();
    } catch (error) {
        ElMessage.error("操作失败");
    }
};

</script>

<style scoped>
.table-box {
    width: 100%;
    height: 100%;
}

.dialog-footer {
    display: flex;
    justify-content: flex-end;
}
</style>